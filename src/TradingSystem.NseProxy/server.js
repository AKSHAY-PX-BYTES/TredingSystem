// Tiny NSE option-chain proxy.
// Deploy this on an India-reachable host (Oracle Cloud Mumbai free VM, Render Singapore,
// Railway, Fly.io ap-south, your own PC, etc.). NSE blocks most foreign/datacenter IPs,
// so the host MUST be able to reach https://www.nseindia.com successfully.
//
// Your TradingSystem.Api points at this via:  "Nse:ProxyBaseUrl": "https://<this-host>/option-chain"
// The API calls:  GET /option-chain?symbol=NIFTY  -> returns NSE's raw option-chain JSON.

import express from "express";

const app = express();
const PORT = process.env.PORT || 8080;

// Optional shared secret. If set, callers must send header `x-proxy-key: <PROXY_KEY>`.
const PROXY_KEY = process.env.PROXY_KEY || "";

// Allowed NSE index symbols (avoid being used as an open proxy).
const ALLOWED = new Set(["NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "NIFTYNXT50"]);

// NSE needs a real browser-like session: first hit a page to get cookies, then call the API.
const BROWSER_HEADERS = {
  "User-Agent":
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
  Accept: "*/*",
  "Accept-Language": "en-US,en;q=0.9",
  "Accept-Encoding": "gzip, deflate, br",
  Referer: "https://www.nseindia.com/option-chain",
  "Sec-Fetch-Site": "same-origin",
  "Sec-Fetch-Mode": "cors",
  Connection: "keep-alive",
};

// Simple in-memory cookie cache + short response cache.
let cookieJar = "";
let cookieFetchedAt = 0;
const COOKIE_TTL_MS = 5 * 60 * 1000; // refresh session cookies every 5 min

const respCache = new Map(); // symbol -> { at, body }
const RESP_TTL_MS = 3000; // 3s cache to be gentle on NSE

async function refreshCookies() {
  // Visit NSE homepage + option-chain page to obtain cookies.
  const res = await fetch("https://www.nseindia.com/option-chain", {
    headers: BROWSER_HEADERS,
  });
  // Collect Set-Cookie headers (Node 18+ exposes getSetCookie()).
  const setCookies =
    typeof res.headers.getSetCookie === "function"
      ? res.headers.getSetCookie()
      : [res.headers.get("set-cookie")].filter(Boolean);
  if (setCookies && setCookies.length) {
    cookieJar = setCookies.map((c) => c.split(";")[0]).join("; ");
    cookieFetchedAt = Date.now();
  }
  // Drain body so the connection is reused cleanly.
  await res.text().catch(() => {});
}

async function ensureCookies() {
  if (!cookieJar || Date.now() - cookieFetchedAt > COOKIE_TTL_MS) {
    await refreshCookies();
  }
}

async function fetchOptionChain(symbol) {
  await ensureCookies();
  const url = `https://www.nseindia.com/api/option-chain-indices?symbol=${encodeURIComponent(
    symbol
  )}`;

  let res = await fetch(url, {
    headers: { ...BROWSER_HEADERS, Cookie: cookieJar },
  });

  // If session expired (401/403), refresh cookies once and retry.
  if (res.status === 401 || res.status === 403) {
    await refreshCookies();
    res = await fetch(url, {
      headers: { ...BROWSER_HEADERS, Cookie: cookieJar },
    });
  }

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    const err = new Error(`NSE responded ${res.status}`);
    err.status = res.status;
    err.body = text.slice(0, 500);
    throw err;
  }

  return res.text();
}

app.get("/health", (_req, res) => res.json({ ok: true, time: new Date().toISOString() }));

app.get("/option-chain", async (req, res) => {
  try {
    if (PROXY_KEY && req.get("x-proxy-key") !== PROXY_KEY) {
      return res.status(401).json({ error: "Unauthorized" });
    }

    const symbol = String(req.query.symbol || "NIFTY").toUpperCase();
    if (!ALLOWED.has(symbol)) {
      return res.status(400).json({ error: `Symbol '${symbol}' not allowed` });
    }

    // Serve short-lived cache to reduce load / rate-limit risk.
    const cached = respCache.get(symbol);
    if (cached && Date.now() - cached.at < RESP_TTL_MS) {
      res.set("Content-Type", "application/json");
      res.set("X-Proxy-Cache", "HIT");
      return res.send(cached.body);
    }

    const body = await fetchOptionChain(symbol);
    respCache.set(symbol, { at: Date.now(), body });

    res.set("Content-Type", "application/json");
    res.set("X-Proxy-Cache", "MISS");
    return res.send(body);
  } catch (e) {
    console.error("option-chain error:", e.status || "", e.message);
    return res
      .status(e.status && e.status >= 400 && e.status < 600 ? 502 : 500)
      .json({ error: "Failed to fetch NSE option chain", detail: e.message });
  }
});

app.listen(PORT, () => {
  console.log(`NSE proxy listening on :${PORT}`);
  console.log(`Try: http://localhost:${PORT}/option-chain?symbol=NIFTY`);
});
