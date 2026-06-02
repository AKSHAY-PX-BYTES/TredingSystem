using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradingSystem.Api.Observability;

/// <summary>
/// Health check for the optional India-hosted NSE option-chain proxy.
/// Reports Healthy when no proxy is configured (feature simply uses fallbacks),
/// Degraded when configured but unreachable, and Healthy when it responds.
/// </summary>
public sealed class NseProxyHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NseProxyHealthCheck> _logger;

    public NseProxyHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NseProxyHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var proxyBaseUrl = _configuration["Nse:ProxyBaseUrl"];
        if (string.IsNullOrWhiteSpace(proxyBaseUrl))
        {
            return HealthCheckResult.Healthy(
                "NSE proxy not configured (using synthetic/fallback data).",
                new Dictionary<string, object> { ["configured"] = false });
        }

        // Derive a base origin to probe (strip a trailing /option-chain path if present).
        if (!Uri.TryCreate(proxyBaseUrl, UriKind.Absolute, out var uri))
        {
            return HealthCheckResult.Degraded(
                $"NSE proxy URL is malformed: {proxyBaseUrl}",
                data: new Dictionary<string, object> { ["configured"] = true });
        }

        var probeUrl = $"{uri.Scheme}://{uri.Authority}/healthz";

        try
        {
            using var client = _httpClientFactory.CreateClient("HealthProbe");
            client.Timeout = TimeSpan.FromSeconds(8);

            using var response = await client.GetAsync(probeUrl, cancellationToken);
            var data = new Dictionary<string, object>
            {
                ["configured"] = true,
                ["url"] = probeUrl,
                ["status"] = (int)response.StatusCode
            };

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("NSE proxy reachable.", data)
                : HealthCheckResult.Degraded(
                    $"NSE proxy returned {(int)response.StatusCode}.", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NSE proxy health probe failed for {Url}", probeUrl);
            return HealthCheckResult.Degraded(
                "NSE proxy unreachable.",
                ex,
                new Dictionary<string, object>
                {
                    ["configured"] = true,
                    ["url"] = probeUrl,
                    ["error"] = ex.Message
                });
        }
    }
}
