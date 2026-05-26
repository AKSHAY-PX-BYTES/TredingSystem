using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace TradingSystem.Api.Services;

/// <summary>
/// Fetches LIVE option chain data from NSE India API.
/// Provides real-time option prices for NIFTY, BANKNIFTY, FINNIFTY, MIDCPNIFTY.
/// </summary>
public interface INseOptionChainService
{
    Task<NseOptionData?> GetOptionPriceAsync(string nseSymbol, decimal strike, string optionType, string expiry);
    Task<NseOptionChainResult?> GetFullChainAsync(string nseSymbol, string? expiry = null);
}

public class NseOptionData
{
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal ImpliedVolatility { get; set; }
    public long OpenInterest { get; set; }
    public long OiChange { get; set; }
    public long Volume { get; set; }
}

public class NseOptionChainResult
{
    public decimal SpotPrice { get; set; }
    public List<string> Expiries { get; set; } = new();
    public Dictionary<string, NseOptionData> Options { get; set; } = new(); // key: "strike_CE" or "strike_PE"
}

public class NseOptionChainService : INseOptionChainService
{
    private readonly ILogger<NseOptionChainService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ConcurrentDictionary<string, (DateTime CachedAt, NseOptionChainResult Data)> _cache = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static CookieContainer _cookies = new();
    private static DateTime _lastCookieRefresh = DateTime.MinValue;
    private const int CACHE_SECONDS = 15; // Refresh every 15 seconds for live data

    // Map our symbols to NSE symbols
    private static readonly Dictionary<string, string> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NIFTY50"] = "NIFTY",
        ["BANKNIFTY"] = "BANKNIFTY",
        ["FINNIFTY"] = "FINNIFTY",
        ["MIDCPNIFTY"] = "MIDCPNIFTY",
        ["SENSEX"] = "SENSEX",  // BSE - different API
        ["BANKEX"] = "BANKEX",  // BSE
    };

    public NseOptionChainService(IHttpClientFactory httpFactory, ILogger<NseOptionChainService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<NseOptionData?> GetOptionPriceAsync(string nseSymbol, decimal strike, string optionType, string expiry)
    {
        var chain = await GetFullChainAsync(nseSymbol, expiry);
        if (chain == null) return null;

        var key = $"{strike:F0}_{optionType}";
        return chain.Options.TryGetValue(key, out var data) ? data : null;
    }

    public async Task<NseOptionChainResult?> GetFullChainAsync(string nseSymbol, string? expiry = null)
    {
        // Map our symbol to NSE symbol
        var nseKey = SymbolMap.TryGetValue(nseSymbol, out var mapped) ? mapped : nseSymbol;
        var cacheKey = $"{nseKey}_{expiry ?? "nearest"}";

        // Return cached if fresh
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            (DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_SECONDS)
        {
            return cached.Data;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue(cacheKey, out cached) &&
                (DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_SECONDS)
            {
                return cached.Data;
            }

            // Ensure we have fresh cookies
            await EnsureCookiesAsync();

            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("Referer", "https://www.nseindia.com/option-chain");
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"https://www.nseindia.com/api/option-chain-indices?symbol={nseKey}";
            
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NSE API returned {Code} for {Symbol}", (int)response.StatusCode, nseKey);
                // Refresh cookies on 401/403
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    _lastCookieRefresh = DateTime.MinValue;
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = ParseOptionChain(json, expiry);

            if (result != null)
            {
                _cache[cacheKey] = (DateTime.UtcNow, result);
                _logger.LogDebug("Fetched NSE option chain for {Symbol}, {Count} options", nseKey, result.Options.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching NSE option chain for {Symbol}", nseSymbol);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureCookiesAsync()
    {
        // Refresh cookies every 5 minutes
        if ((DateTime.UtcNow - _lastCookieRefresh).TotalMinutes < 5) return;

        try
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.Timeout = TimeSpan.FromSeconds(10);

            // Hit NSE main page to get cookies (nseappid, nsit, bm_sv etc.)
            await client.GetAsync("https://www.nseindia.com/option-chain");
            _lastCookieRefresh = DateTime.UtcNow;
            _logger.LogDebug("Refreshed NSE cookies");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh NSE cookies");
        }
    }

    private NseOptionChainResult? ParseOptionChain(string json, string? targetExpiry)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("records", out var records)) return null;

            var result = new NseOptionChainResult();

            // Get spot price
            if (records.TryGetProperty("underlyingValue", out var underlying))
                result.SpotPrice = underlying.GetDecimal();

            // Get expiry dates
            if (records.TryGetProperty("expiryDates", out var expiryDates))
            {
                foreach (var exp in expiryDates.EnumerateArray())
                    result.Expiries.Add(exp.GetString() ?? "");
            }

            // Determine which expiry to filter
            var filterExpiry = targetExpiry;
            if (string.IsNullOrEmpty(filterExpiry) && result.Expiries.Count > 0)
                filterExpiry = result.Expiries[0]; // nearest expiry

            // Parse the nearest expiry or convert our format to NSE format
            DateTime? targetDate = null;
            if (!string.IsNullOrEmpty(filterExpiry))
            {
                if (DateTime.TryParse(filterExpiry, out var parsed))
                    targetDate = parsed.Date;
            }

            // Parse option data
            if (records.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                {
                    // Filter by expiry if specified
                    if (targetDate.HasValue && item.TryGetProperty("expiryDate", out var expProp))
                    {
                        if (DateTime.TryParse(expProp.GetString(), out var itemExp) && itemExp.Date != targetDate.Value)
                            continue;
                    }

                    // Parse CE
                    if (item.TryGetProperty("CE", out var ce))
                    {
                        var strike = ce.GetProperty("strikePrice").GetDecimal();
                        var key = $"{strike:F0}_CE";
                        result.Options[key] = ParseOptionItem(ce);
                    }

                    // Parse PE
                    if (item.TryGetProperty("PE", out var pe))
                    {
                        var strike = pe.GetProperty("strikePrice").GetDecimal();
                        var key = $"{strike:F0}_PE";
                        result.Options[key] = ParseOptionItem(pe);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing NSE option chain JSON");
            return null;
        }
    }

    private NseOptionData ParseOptionItem(JsonElement item)
    {
        return new NseOptionData
        {
            LastPrice = GetDecimalSafe(item, "lastPrice"),
            Change = GetDecimalSafe(item, "change"),
            ChangePercent = GetDecimalSafe(item, "pchangeInOI") != 0 ? GetDecimalSafe(item, "pChange") : 0,
            BidPrice = GetDecimalSafe(item, "bidprice"),
            AskPrice = GetDecimalSafe(item, "askPrice"),
            ImpliedVolatility = GetDecimalSafe(item, "impliedVolatility"),
            OpenInterest = GetLongSafe(item, "openInterest"),
            OiChange = GetLongSafe(item, "changeinOpenInterest"),
            Volume = GetLongSafe(item, "totalTradedVolume"),
        };
    }

    private static decimal GetDecimalSafe(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal();
            if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var d)) return d;
        }
        return 0;
    }

    private static long GetLongSafe(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number) return val.GetInt64();
            if (val.ValueKind == JsonValueKind.String && long.TryParse(val.GetString(), out var l)) return l;
        }
        return 0;
    }
}
