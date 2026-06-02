// Tiny NSE option-chain proxy.
// Deploy this on an India-reachable host (Render Singapore, Railway, your own PC via
// Cloudflare Tunnel, etc.). NSE blocks most foreign/datacenter IPs, so the host MUST
// be able to reach https://www.nseindia.com successfully.
//
// Your TradingSystem.Api points at this via:  "Nse:ProxyBaseUrl": "https://<this-host>/option-chain"
// The API calls:  GET /option-chain?symbol=NIFTY  -> returns NSE's raw option-chain JSON.

import express from "express";
import client from "prom-client";

const app = express();
const PORT = process.env.PORT || 8080;

// ---- Prometheus metrics ----------------------------------------------------
const registry = new client.Registry();
registry.setDefaultLabels({ service: "tradingsystem-nse-proxy" });
client.collectDefaultMetrics({ register: registry }); // process/gc/event-loop

const httpRequests = new client.Counter({
  name: "nse_proxy_http_requests_total",
  help: "Total HTTP requests handled by the NSE proxy",
  labelNames: ["method", "route", "status"],
  registers: [registry],
});

const nseFetches = new client.Counter({
  name: "nse_proxy_upstream_fetch_total",
  help: "Outcome of upstream NSE option-chain fetches",
  labelNames: ["symbol", "result"], // result = success | error | cache_hit
  registers: [registry],
});

const fetchDuration = new client.Histogram({
  name: "nse_proxy_upstream_fetch_duration_seconds",
  help: "Duration of upstream NSE option-chain fetches",
  labelNames: ["symbol"],
  buckets: [0.1, 0.25, 0.5, 1, 2, 5, 10],
  registers: [registry],
});

const cookieAge = new client.Gauge({
  name: "nse_proxy_cookie_age_seconds",
  help: "Age of the cached NSE session cookies in seconds",
  registers: [registry],
});

// Count every request once the response finishes.
app.use((req, res, next) => {
  res.on("finish", () => {
    const route = req.route?.path || req.path || "unknown";
    httpRequests.inc({ method: req.method, route, status: res.statusCode });
  });
  next();
});

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
  // NSE only returns API data to sessions that look like a real browser flow.
  // Warm up by visiting the homepage first, then the option-chain page,
  // accumulating cookies across both requests.
  cookieJar = "";
  const warmupUrls = [
    "https://www.nseindia.com/",
    "https://www.nseindia.com/option-chain",
  ];

  const jar = {};
  for (const url of warmupUrls) {
    try {
      const res = await fetch(url, {
        headers: {
          ...BROWSER_HEADERS,
          Accept:
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
          "Sec-Fetch-Dest": "document",
          "Sec-Fetch-Mode": "navigate",
          "Upgrade-Insecure-Requests": "1",
          Cookie: serializeJar(jar),
        },
        redirect: "follow",
      });
      const setCookies =
        typeof res.headers.getSetCookie === "function"
          ? res.headers.getSetCookie()
          : [res.headers.get("set-cookie")].filter(Boolean);
      for (const c of setCookies) {
        if (!c) continue;
        const [pair] = c.split(";");
        const idx = pair.indexOf("=");
        if (idx > 0) jar[pair.slice(0, idx).trim()] = pair.slice(idx + 1).trim();
      }
      await res.text().catch(() => {});
    } catch (e) {
      console.error("warmup fetch failed:", url, e.message);
    }
  }

  cookieJar = serializeJar(jar);
  cookieFetchedAt = Date.now();
}

function serializeJar(jar) {
  return Object.entries(jar)
    .map(([k, v]) => `${k}=${v}`)
    .join("; ");
}

async function ensureCookies() {
  if (!cookieJar || Date.now() - cookieFetchedAt > COOKIE_TTL_MS) {
    await refreshCookies();
  }
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function fetchOptionChain(symbol) {
  await ensureCookies();
  const url = `https://www.nseindia.com/api/option-chain-indices?symbol=${encodeURIComponent(
    symbol
  )}`;

  const apiHeaders = () => ({
    ...BROWSER_HEADERS,
    Accept: "application/json, text/plain, */*",
    "Sec-Fetch-Dest": "empty",
    "Sec-Fetch-Mode": "cors",
    "X-Requested-With": "XMLHttpRequest",
    Cookie: cookieJar,
  });

  // Akamai sometimes lets the 2nd/3rd call through within a primed session.
  let res;
  for (let attempt = 1; attempt <= 3; attempt++) {
    // Prime a lightweight API first to help validate the session.
    try {
      await fetch("https://www.nseindia.com/api/marketStatus", {
        headers: apiHeaders(),
      }).then((r) => r.text().catch(() => {}));
    } catch {}

    res = await fetch(url, { headers: apiHeaders() });
    if (res.ok) break;

    // Refresh cookies and back off before retrying.
    await refreshCookies();
    await sleep(400 * attempt);
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

// Alias used by the TradingSystem.Api health check (NseProxyHealthCheck probes /healthz).
app.get("/healthz", (_req, res) =>
  res.json({
    ok: true,
    service: "tradingsystem-nse-proxy",
    cookieAgeSeconds: cookieFetchedAt ? Math.round((Date.now() - cookieFetchedAt) / 1000) : null,
    time: new Date().toISOString(),
  })
);

// Prometheus scrape endpoint.
app.get("/metrics", async (_req, res) => {
  try {
    cookieAge.set(cookieFetchedAt ? (Date.now() - cookieFetchedAt) / 1000 : 0);
    res.set("Content-Type", registry.contentType);
    res.send(await registry.metrics());
  } catch (e) {
    res.status(500).send(e.message);
  }
});

// Diagnostic endpoint: shows whether this host can reach NSE and what it returns.
app.get("/debug", async (_req, res) => {
  const out = { time: new Date().toISOString() };
  try {
    await refreshCookies();
    out.cookieCount = cookieJar ? cookieJar.split(";").length : 0;
    out.cookiePreview = cookieJar.slice(0, 80);

    const url =
      "https://www.nseindia.com/api/option-chain-indices?symbol=NIFTY";
    const r = await fetch(url, {
      headers: { ...BROWSER_HEADERS, Cookie: cookieJar },
    });
    out.nseStatus = r.status;
    const text = await r.text().catch(() => "");
    out.bodyPreview = text.slice(0, 200);
    out.looksLikeJson = text.trimStart().startsWith("{");
  } catch (e) {
    out.error = e.message;
  }
  res.json(out);
});

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
      nseFetches.inc({ symbol, result: "cache_hit" });
      res.set("Content-Type", "application/json");
      res.set("X-Proxy-Cache", "HIT");
      return res.send(cached.body);
    }

    const stopTimer = fetchDuration.startTimer({ symbol });
    const body = await fetchOptionChain(symbol);
    stopTimer();
    nseFetches.inc({ symbol, result: "success" });
    respCache.set(symbol, { at: Date.now(), body });

    res.set("Content-Type", "application/json");
    res.set("X-Proxy-Cache", "MISS");
    return res.send(body);
  } catch (e) {
    nseFetches.inc({ symbol: String(req.query.symbol || "NIFTY").toUpperCase(), result: "error" });
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