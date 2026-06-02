using System.Diagnostics.Metrics;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace TradingSystem.Api.Observability;

/// <summary>
/// Centralized health-check + OpenTelemetry/Prometheus wiring so a single Grafana
/// view can show which services are up, plus request/runtime usage metrics.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>Service name surfaced as a Prometheus/OTel resource label.</summary>
    public const string ServiceName = "tradingsystem-api";

    /// <summary>Custom meter for app-specific counters (login attempts, signals, etc.).</summary>
    public static readonly Meter AppMeter = new("TradingSystem.Api", "1.0.0");

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // A dedicated HttpClient for health probes (NSE proxy etc.).
        services.AddHttpClient("HealthProbe");

        // ---- Health checks -------------------------------------------------
        var dbConnStr = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        var healthChecks = services.AddHealthChecks()
            // Liveness: process is up and responding.
            .AddCheck("self", () => HealthCheckResult.Healthy("API process alive."),
                tags: new[] { "live" })
            // The NSE option-chain proxy (optional dependency).
            .AddCheck<NseProxyHealthCheck>("nse-proxy",
                tags: new[] { "ready", "dependency" });

        // Readiness: database connectivity (only if a conn string exists).
        if (!string.IsNullOrWhiteSpace(dbConnStr))
        {
            var npgsqlConn = NormalizePostgresConnectionString(dbConnStr);
            healthChecks.AddNpgSql(
                connectionString: npgsqlConn,
                name: "postgres",
                tags: new[] { "ready", "db" });
        }

        // ---- OpenTelemetry metrics + Prometheus scrape endpoint ------------
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ServiceName,
                serviceVersion: "1.0.0"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()   // http.server.request.duration, etc.
                    .AddHttpClientInstrumentation()   // outbound calls (Yahoo, NSE, Razorpay)
                    .AddRuntimeInstrumentation()      // GC, heap, threadpool
                    .AddMeter(AppMeter.Name)          // custom app metrics
                    .AddPrometheusExporter();         // exposes /metrics
            });

        return services;
    }

    /// <summary>
    /// Maps /health (overall), /health/live (liveness), /health/ready (readiness),
    /// and /metrics (Prometheus). Health endpoints return rich JSON.
    /// </summary>
    public static WebApplication MapObservability(this WebApplication app)
    {
        var jsonOptions = new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        };

        // Overall health (all checks).
        app.MapHealthChecks("/health", jsonOptions).AllowAnonymous();

        // Liveness — is the process up? (no dependencies)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = reg => reg.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        // Readiness — can it serve traffic? (db + key dependencies)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = reg => reg.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        // Prometheus scrape endpoint -> /metrics
        app.MapPrometheusScrapingEndpoint();

        return app;
    }

    /// <summary>
    /// Neon supplies postgres:// URLs; Npgsql health check needs Host=... format.
    /// Mirrors the conversion already done for the EF Core DbContext.
    /// </summary>
    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("postgres://") ||
            connectionString.StartsWith("postgresql://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
                   $"Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};" +
                   $"Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;" +
                   "Timeout=60;Command Timeout=60";
        }

        return connectionString.Contains("Timeout=")
            ? connectionString
            : connectionString + ";Timeout=60;Command Timeout=60";
    }
}
