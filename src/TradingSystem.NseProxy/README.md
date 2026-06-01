# TradingSystem NSE Proxy

A tiny Node.js proxy that returns NSE's option-chain JSON. Your main API
(`TradingSystem.Api`) can't reach `nseindia.com` from a US/foreign host because NSE
blocks datacenter/foreign IPs. This proxy runs on an **India-reachable** host and does
the NSE cookie/session handshake for you.

```
TradingSystem.Api (Render, US)  ──►  This proxy (India IP)  ──►  nseindia.com
        Nse:ProxyBaseUrl                  /option-chain
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Liveness check |
| GET | `/option-chain?symbol=NIFTY` | Raw NSE option-chain JSON (NIFTY, BANKNIFTY, FINNIFTY, MIDCPNIFTY, NIFTYNXT50) |

Optional auth: set `PROXY_KEY` env var; callers must send header `x-proxy-key: <key>`.
(The .NET service currently sends no key — only set `PROXY_KEY` if you also add the header there.)

## Run locally

```powershell
cd src/TradingSystem.NseProxy
npm install
npm start
# open http://localhost:8080/option-chain?symbol=NIFTY
```

> Note: from outside India this will often return an NSE 401/403. That's expected —
> it must be deployed where NSE is reachable.

## Where to host (must be India-reachable & free/cheap)

These regions/providers generally reach NSE successfully:

1. **Oracle Cloud Free Tier — Mumbai/Hyderabad** (best free option: always-free Arm VM)
   - Create an Always-Free VM in the `ap-mumbai-1` region.
   - Install Node 20, copy this folder, run `npm install && npm start` (use `pm2` or a systemd unit to keep it alive).
2. **Railway / Render / Fly.io** with an **ap-south / Singapore** region (Singapore usually works; US does not).
   - Fly.io: `fly launch` then set primary region to `sin` or `bom`.
3. **Your own PC / Raspberry Pi at home** (Indian broadband)
   - Run `npm start`, then expose it with Cloudflare Tunnel or ngrok to get a public HTTPS URL.

### Docker

```powershell
docker build -t nse-proxy .
docker run -p 8080:8080 nse-proxy
```

## Wire it into TradingSystem.Api

Set the proxy URL (no trailing `?symbol=` — the API appends it):

- Local dev — `src/TradingSystem.Api/appsettings.json`:
  ```json
  "Nse": { "ProxyBaseUrl": "http://localhost:8080/option-chain" }
  ```
- Production (Render) — set an environment variable instead of editing the file:
  ```
  Nse__ProxyBaseUrl = https://your-india-proxy.example.com/option-chain
  ```
  (Render env vars use `__` to represent the `:` in config keys.)

Once set, `NseOptionChainService` uses the proxy as the **primary live source**, so the
options chain shows accurate, Groww-matching prices. You can then set
`Fno:AllowSyntheticData=false` to fully remove estimated values.

## How it works

1. On first request (or every 5 min) it visits `https://www.nseindia.com/option-chain`
   to obtain session cookies.
2. It calls `https://www.nseindia.com/api/option-chain-indices?symbol=...` with those
   cookies and browser-like headers.
3. On 401/403 it refreshes cookies once and retries.
4. Responses are cached for 3 seconds to be gentle on NSE.
