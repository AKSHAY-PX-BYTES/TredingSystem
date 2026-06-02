# TradingSystem — Centralized Observability

One Grafana view that shows **which service is up/down**, plus request, runtime,
and upstream-data **usage metrics** across the whole stack.

## What's instrumented

| Service | Health endpoint | Metrics endpoint | Signal source |
|---|---|---|---|
| `TradingSystem.Api` (.NET 8) | `/health`, `/health/live`, `/health/ready` | `/metrics` | OpenTelemetry (ASP.NET Core, HttpClient, .NET runtime) + Prometheus exporter |
| `TradingSystem.NseProxy` (Node) | `/healthz` | `/metrics` | `prom-client` (default + custom NSE fetch metrics) |
| `TradingSystem.Web` (Blazor WASM) | static site | — | Blackbox HTTP probe (up/down) |
| `TradingSystem.Angular` | static site | — | Blackbox HTTP probe (up/down) |
| PostgreSQL (Neon) | via API `/health/ready` | — | API health check (`AddNpgSql`) |

The API health checks include:
- **self** (liveness) — process is alive
- **postgres** (readiness) — database connectivity
- **nse-proxy** (readiness/dependency) — probes the configured `Nse:ProxyBaseUrl`

## Quick start (local Grafana + Prometheus + Blackbox)

```powershell
cd observability
docker compose up -d
```

Then open:
- Grafana → http://localhost:3000  (login `admin` / `admin`)
- Dashboard → **TradingSystem → Service Health & Usage** (auto-provisioned)
- Prometheus → http://localhost:9090

The dashboard data source is auto-provisioned, so it works immediately.

## Configure your targets

Edit `prometheus/prometheus.yml` and set the real hostnames:
- `tradingsystem-api` job → your API host (default `tredingsystem-api.onrender.com`)
- `tradingsystem-nse-proxy` job → your NSE proxy host
- `blackbox-http` targets → API `/health`, frontend URL(s), proxy `/healthz`

Restart Prometheus after edits: `docker compose restart prometheus`.

## Verify the endpoints

```powershell
# API
curl https://tredingsystem-api.onrender.com/health
curl https://tredingsystem-api.onrender.com/health/ready
curl https://tredingsystem-api.onrender.com/metrics

# NSE proxy
curl https://<your-nse-proxy-host>/healthz
curl https://<your-nse-proxy-host>/metrics
```

## Importing the dashboard manually (Grafana Cloud / existing Grafana)

If you don't want to run the compose stack, import the JSON directly:

1. Grafana → Dashboards → **New → Import**
2. Upload `grafana/dashboards/tradingsystem-overview.json`
3. Pick your Prometheus data source when prompted.

> Grafana Cloud (free tier) gives you hosted Grafana + Prometheus + Loki +
> Synthetic Monitoring with no infra to run — point your scrape config / agent
> at the same `/metrics` endpoints.

## Key metrics used by the dashboard

| Panel | PromQL (core) |
|---|---|
| Services Up/Down | `probe_success` |
| Uptime % | `avg_over_time(probe_success[$__range])` |
| API request rate | `rate(http_server_request_duration_seconds_count{job="tradingsystem-api"}[5m])` |
| API latency p95 | `histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))` |
| API 5xx error rate | 5xx count / total count |
| GC heap | `process_runtime_dotnet_gc_heap_size_bytes` |
| NSE fetch outcomes | `rate(nse_proxy_upstream_fetch_total[5m])` |
| NSE cookie age | `nse_proxy_cookie_age_seconds` |

## Adding centralized logs (optional, Loki)

The compose stack already runs Loki + provisions the data source. To ship .NET
logs to Loki, add the Serilog Loki sink to `TradingSystem.Api`:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
<PackageReference Include="Serilog.Sinks.Grafana.Loki" Version="8.3.0" />
```

```csharp
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        Environment.GetEnvironmentVariable("LOKI_URL") ?? "http://localhost:3100",
        labels: new[] { new LokiLabel { Key = "service", Value = "tradingsystem-api" } }));
```

Then add a **Logs** panel in Grafana using the Loki data source with
`{service="tradingsystem-api"}`.

## Notes for Render deployment

- Expose `/metrics` publicly (it is, by default). If you want to protect it,
  put it behind a network rule or a reverse proxy and scrape internally.
- `/metrics` is anonymous (no JWT) so Prometheus can scrape it.
- On Render free tier the API sleeps when idle; the blackbox probe will report
  it DOWN during cold start — that's expected.
