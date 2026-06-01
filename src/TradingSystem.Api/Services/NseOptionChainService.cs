using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace TradingSystem.Api.Services;

/// <summary>
/// Fetches LIVE option chain data using multiple sources:
/// 1. Yahoo Finance Options API (works globally, real-time, no IP restrictions)
/// 2. NSE India API as fallback (Indian IPs only)
/// </summary>
public interface INseOptionChainService
{
    Task<NseOptionData?> GetOptionPriceAsync(string symbol, decimal strike, string optionType, string expiry);
    Task<NseOptionChainResult?> GetFullChainAsync(string symbol, string? expiry = null);
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
    private readonly string? _proxyBaseUrl;
    private readonly ConcurrentDictionary<string, (DateTime CachedAt, NseOptionChainResult Data)> _cache = new();
    private static readonly SemaphoreSlim _semaphore = new(2, 2);
    private const int CACHE_SECONDS = 10; // 10-second cache for near real-time

    // Yahoo Finance option chain tickers
    private static readonly Dictionary<string, string> YahooOptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NIFTY50"] = "^NSEI",
        ["BANKNIFTY"] = "^NSEBANK",
        ["FINNIFTY"] = "NIFTY_FIN_SERVICE.NS",
        ["MIDCPNIFTY"] = "NIFTY_MID_SELECT.NS",
        ["SENSEX"] = "^BSESN",
        ["SPX500"] = "^GSPC",
        ["NASDAQ"] = "^IXIC",
        ["DOWJONES"] = "^DJI",
        ["NIFTYIT"] = "^CNXIT",
        ["NIFTYPHARMA"] = "^CNXPHARMA",
    };

    // NSE direct API symbols
    private static readonly Dictionary<string, string> NseSymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NIFTY50"] = "NIFTY",
        ["BANKNIFTY"] = "BANKNIFTY",
        ["FINNIFTY"] = "FINNIFTY",
        ["MIDCPNIFTY"] = "MIDCPNIFTY",
    };

    public NseOptionChainService(IHttpClientFactory httpFactory, ILogger<NseOptionChainService> logger, IConfiguration configuration)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        // Optional India-hosted proxy that returns NSE's option-chain JSON.
        // Set "Nse:ProxyBaseUrl" to a reachable endpoint, e.g. "https://my-india-proxy.example.com/option-chain"
        _proxyBaseUrl = configuration["Nse:ProxyBaseUrl"];
    }

    public async Task<NseOptionData?> GetOptionPriceAsync(string symbol, decimal strike, string optionType, string expiry)
    {
        var chain = await GetFullChainAsync(symbol, expiry);
        if (chain == null) return null;

        var key = $"{strike:F0}_{optionType}";
        return chain.Options.TryGetValue(key, out var data) ? data : null;
    }

    public async Task<NseOptionChainResult?> GetFullChainAsync(string symbol, string? expiry = null)
    {
        var cacheKey = $"{symbol}_{expiry ?? "nearest"}";

        // Return cached if fresh (10 seconds)
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            (DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_SECONDS)
        {
            return cached.Data;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after lock
            if (_cache.TryGetValue(cacheKey, out cached) &&
                (DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_SECONDS)
            {
                return cached.Data;
            }

            // Try Yahoo Finance Options API first (works globally, real-time)
            var result = await FetchFromYahooOptionsAsync(symbol, expiry);
            
            if (result != null && result.Options.Count > 0)
            {
                _cache[cacheKey] = (DateTime.UtcNow, result);
                _logger.LogInformation("LIVE options from Yahoo for {Symbol}: {Count} contracts, spot={Spot}",
                    symbol, result.Options.Count, result.SpotPrice);
                return result;
            }

            // Most accurate for Indian indices: a configured India-hosted proxy returning NSE JSON
            result = await FetchFromProxyAsync(symbol, expiry);
            if (result != null && result.Options.Count > 0)
            {
                _cache[cacheKey] = (DateTime.UtcNow, result);
                _logger.LogInformation("LIVE options from proxy for {Symbol}: {Count} contracts", symbol, result.Options.Count);
                return result;
            }

            // Fallback: Try NSE direct API (works from Indian IPs only)
            result = await FetchFromNseDirectAsync(symbol, expiry);
            if (result != null && result.Options.Count > 0)
            {
                _cache[cacheKey] = (DateTime.UtcNow, result);
                _logger.LogInformation("LIVE options from NSE for {Symbol}: {Count} contracts", symbol, result.Options.Count);
                return result;
            }

            _logger.LogWarning("No live option data available for {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching option chain for {Symbol}", symbol);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Yahoo Finance v7 Options API - real-time option chain.
    /// Works globally without IP restrictions.
    /// URL: https://query1.finance.yahoo.com/v7/finance/options/{symbol}
    /// </summary>
    private async Task<NseOptionChainResult?> FetchFromYahooOptionsAsync(string symbol, string? expiry)
    {
        var yahooTicker = YahooOptionMap.TryGetValue(symbol, out var mapped) ? mapped : null;
        if (yahooTicker == null) return null;

        try
        {
            var client = _httpFactory.CreateClient("YahooFinance");
            var encoded = Uri.EscapeDataString(yahooTicker);
            
            // Build URL - if specific expiry, convert to Unix timestamp
            var url = $"v7/finance/options/{encoded}";
            if (!string.IsNullOrEmpty(expiry) && DateTime.TryParse(expiry, out var expiryDate))
            {
                var epoch = new DateTimeOffset(expiryDate.Date.AddHours(15.5)).ToUnixTimeSeconds();
                url += $"?date={epoch}";
            }

            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Options returned {Code} for {Symbol}", (int)response.StatusCode, yahooTicker);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseYahooOptionsResponse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo Options API error for {Symbol}", symbol);
            return null;
        }
    }

    private NseOptionChainResult? ParseYahooOptionsResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("optionChain", out var optionChain)) return null;
            if (!optionChain.TryGetProperty("result", out var results)) return null;
            
            var resultArr = results.EnumerateArray().ToList();
            if (resultArr.Count == 0) return null;
            
            var first = resultArr[0];
            var result = new NseOptionChainResult();

            // Get spot/underlying price from quote
            if (first.TryGetProperty("quote", out var quote))
            {
                if (quote.TryGetProperty("regularMarketPrice", out var price))
                    result.SpotPrice = price.GetDecimal();
            }

            // Get expiry dates (unix timestamps → formatted strings)
            if (first.TryGetProperty("expirationDates", out var expirations))
            {
                foreach (var exp in expirations.EnumerateArray())
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()).DateTime;
                    result.Expiries.Add(dt.ToString("dd-MMM-yyyy"));
                }
            }

            // Parse options for the requested expiry
            if (first.TryGetProperty("options", out var options))
            {
                foreach (var optionSet in options.EnumerateArray())
                {
                    // Parse calls
                    if (optionSet.TryGetProperty("calls", out var calls))
                    {
                        foreach (var call in calls.EnumerateArray())
                        {
                            var strike = GetDecimalProp(call, "strike");
                            var key = $"{strike:F0}_CE";
                            result.Options[key] = ParseYahooOption(call);
                        }
                    }

                    // Parse puts
                    if (optionSet.TryGetProperty("puts", out var puts))
                    {
                        foreach (var put in puts.EnumerateArray())
                        {
                            var strike = GetDecimalProp(put, "strike");
                            var key = $"{strike:F0}_PE";
                            result.Options[key] = ParseYahooOption(put);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Yahoo options JSON");
            return null;
        }
    }

    private NseOptionData ParseYahooOption(JsonElement el)
    {
        return new NseOptionData
        {
            LastPrice = GetDecimalProp(el, "lastPrice"),
            Change = GetDecimalProp(el, "change"),
            ChangePercent = GetDecimalProp(el, "percentChange"),
            BidPrice = GetDecimalProp(el, "bid"),
            AskPrice = GetDecimalProp(el, "ask"),
            ImpliedVolatility = Math.Round(GetDecimalProp(el, "impliedVolatility") * 100, 2), // Yahoo gives 0.xx format
            OpenInterest = GetLongProp(el, "openInterest"),
            OiChange = 0, // Yahoo doesn't provide OI change directly
            Volume = GetLongProp(el, "volume"),
        };
    }

    /// <summary>
    /// Optional India-hosted proxy returning NSE's standard option-chain JSON
    /// (same shape as https://www.nseindia.com/api/option-chain-indices?symbol=NIFTY).
    /// This is the recommended way to get accurate live Indian option data from a cloud server.
    /// </summary>
    private async Task<NseOptionChainResult?> FetchFromProxyAsync(string symbol, string? expiry)
    {
        if (string.IsNullOrWhiteSpace(_proxyBaseUrl)) return null;
        if (!NseSymbolMap.TryGetValue(symbol, out var nseKey)) return null;

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var sep = _proxyBaseUrl.Contains('?') ? "&" : "?";
            var url = $"{_proxyBaseUrl}{sep}symbol={Uri.EscapeDataString(nseKey)}";

            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Proxy returned {Status} for {Symbol}", response.StatusCode, symbol);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseNseResponse(json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Proxy option-chain fetch failed for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// NSE India direct API - fallback for Indian-IP deployments.
    /// </summary>
    private async Task<NseOptionChainResult?> FetchFromNseDirectAsync(string symbol, string? expiry)
    {
        if (!NseSymbolMap.TryGetValue(symbol, out var nseKey)) return null;

        try
        {
            using var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            // Get session cookies
            try { await client.GetAsync("https://www.nseindia.com/option-chain"); } catch { }

            client.DefaultRequestHeaders.Add("Referer", "https://www.nseindia.com/option-chain");
            var url = $"https://www.nseindia.com/api/option-chain-indices?symbol={nseKey}";
            
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseNseResponse(json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "NSE direct API unavailable for {Symbol}", symbol);
            return null;
        }
    }

    private NseOptionChainResult? ParseNseResponse(string json, string? targetExpiry)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("records", out var records)) return null;

            var result = new NseOptionChainResult();

            if (records.TryGetProperty("underlyingValue", out var uv))
                result.SpotPrice = uv.GetDecimal();

            if (records.TryGetProperty("expiryDates", out var expDates))
                foreach (var e in expDates.EnumerateArray())
                    result.Expiries.Add(e.GetString() ?? "");

            var filterExpiry = targetExpiry ?? (result.Expiries.Count > 0 ? result.Expiries[0] : null);
            DateTime? targetDate = null;
            if (!string.IsNullOrEmpty(filterExpiry) && DateTime.TryParse(filterExpiry, out var pd))
                targetDate = pd.Date;

            if (records.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (targetDate.HasValue && item.TryGetProperty("expiryDate", out var expProp))
                    {
                        if (DateTime.TryParse(expProp.GetString(), out var itemExp) && itemExp.Date != targetDate.Value)
                            continue;
                    }

                    if (item.TryGetProperty("CE", out var ce))
                    {
                        var strike = ce.GetProperty("strikePrice").GetDecimal();
                        result.Options[$"{strike:F0}_CE"] = ParseNseOption(ce);
                    }

                    if (item.TryGetProperty("PE", out var pe))
                    {
                        var strike = pe.GetProperty("strikePrice").GetDecimal();
                        result.Options[$"{strike:F0}_PE"] = ParseNseOption(pe);
                    }
                }
            }

            return result;
        }
        catch { return null; }
    }

    private NseOptionData ParseNseOption(JsonElement item)
    {
        return new NseOptionData
        {
            LastPrice = GetDecimalProp(item, "lastPrice"),
            Change = GetDecimalProp(item, "change"),
            ChangePercent = GetDecimalProp(item, "pChange"),
            BidPrice = GetDecimalProp(item, "bidprice"),
            AskPrice = GetDecimalProp(item, "askPrice"),
            ImpliedVolatility = GetDecimalProp(item, "impliedVolatility"),
            OpenInterest = GetLongProp(item, "openInterest"),
            OiChange = GetLongProp(item, "changeinOpenInterest"),
            Volume = GetLongProp(item, "totalTradedVolume"),
        };
    }

    private static decimal GetDecimalProp(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal();
            if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var d)) return d;
        }
        return 0;
    }

    private static long GetLongProp(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number) return val.GetInt64();
            if (val.ValueKind == JsonValueKind.String && long.TryParse(val.GetString(), out var l)) return l;
        }
        return 0;
    }
}
