using System.Collections.Concurrent;
using System.Text.Json;

namespace TradingSystem.Api.Services;

/// <summary>
/// Fetches real-time stock quotes from Yahoo Finance v8 chart API.
/// No API key required. Covers NSE (.NS), BSE (.BO), US, LSE (.L), TSE (.T) markets.
/// </summary>
public interface ILiveMarketDataService
{
    Task<LiveStockData?> FetchQuoteAsync(string yahooSymbol);
    Task<Dictionary<string, LiveStockData>> FetchBatchAsync(IEnumerable<string> yahooSymbols);
}

public class LiveStockData
{
    public decimal Price { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
    public List<decimal> Sparkline { get; set; } = new();
}

public class YahooFinanceService : ILiveMarketDataService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<YahooFinanceService> _logger;
    private readonly ConcurrentDictionary<string, (DateTime CachedAt, LiveStockData Data)> _cache = new();
    private static readonly SemaphoreSlim _throttle = new(5, 5); // max 5 concurrent Yahoo requests
    private const int CACHE_SECONDS = 30;

    public YahooFinanceService(IHttpClientFactory httpFactory, ILogger<YahooFinanceService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<LiveStockData?> FetchQuoteAsync(string yahooSymbol)
    {
        // Return cached if fresh
        if (_cache.TryGetValue(yahooSymbol, out var cached) &&
            (DateTime.UtcNow - cached.CachedAt).TotalSeconds < CACHE_SECONDS)
        {
            return cached.Data;
        }

        await _throttle.WaitAsync();
        try
        {
            var client = _httpFactory.CreateClient("YahooFinance");
            var encoded = Uri.EscapeDataString(yahooSymbol);
            var url = $"v8/finance/chart/{encoded}?interval=5m&range=1d&includePrePost=false";

            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance returned {Code} for {Symbol}",
                    (int)response.StatusCode, yahooSymbol);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = ParseChartResponse(json);

            if (data != null)
            {
                _cache[yahooSymbol] = (DateTime.UtcNow, data);
                _logger.LogDebug("Fetched live quote for {Symbol}: {Price}", yahooSymbol, data.Price);
            }

            return data;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout fetching {Symbol}", yahooSymbol);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Network error fetching {Symbol}: {Msg}", yahooSymbol, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error fetching {Symbol}", yahooSymbol);
            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    public async Task<Dictionary<string, LiveStockData>> FetchBatchAsync(IEnumerable<string> yahooSymbols)
    {
        var results = new ConcurrentDictionary<string, LiveStockData>();
        var symbols = yahooSymbols.Where(s => !string.IsNullOrEmpty(s)).ToList();

        // Fetch all in parallel (throttled by semaphore)
        var tasks = symbols.Select(async symbol =>
        {
            var quote = await FetchQuoteAsync(symbol);
            if (quote != null)
                results[symbol] = quote;
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Batch fetch: {Success}/{Total} quotes retrieved",
            results.Count, symbols.Count);

        return new Dictionary<string, LiveStockData>(results);
    }

    private LiveStockData? ParseChartResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var chart = doc.RootElement.GetProperty("chart");

            // Check for API error
            if (chart.TryGetProperty("error", out var errProp) &&
                errProp.ValueKind != JsonValueKind.Null)
                return null;

            var results = chart.GetProperty("result");
            if (results.GetArrayLength() == 0) return null;

            var result = results[0];
            var meta = result.GetProperty("meta");

            // Extract price data from meta
            var price = GetDecimal(meta, "regularMarketPrice");
            if (price == 0) return null;

            var prevClose = GetDecimal(meta, "chartPreviousClose");
            if (prevClose == 0) prevClose = GetDecimal(meta, "previousClose");
            if (prevClose == 0) prevClose = price;

            var high = GetDecimal(meta, "regularMarketDayHigh");
            if (high == 0) high = price;

            var low = GetDecimal(meta, "regularMarketDayLow");
            if (low == 0) low = price;

            var volume = GetLong(meta, "regularMarketVolume");

            // Extract intraday close prices for sparkline
            var sparkline = new List<decimal>();
            if (result.TryGetProperty("indicators", out var indicators) &&
                indicators.TryGetProperty("quote", out var quoteArr) &&
                quoteArr.GetArrayLength() > 0 &&
                quoteArr[0].TryGetProperty("close", out var closes))
            {
                foreach (var item in closes.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                        sparkline.Add(Math.Round(item.GetDecimal(), 2));
                }
            }

            // Downsample to ~20 points for compact sparkline
            if (sparkline.Count > 20)
            {
                var step = (double)sparkline.Count / 20;
                sparkline = Enumerable.Range(0, 20)
                    .Select(i => sparkline[Math.Min((int)(i * step), sparkline.Count - 1)])
                    .ToList();
            }

            // Ensure at least one data point
            if (sparkline.Count == 0)
                sparkline.Add(price);

            return new LiveStockData
            {
                Price = price,
                PreviousClose = prevClose,
                High = high,
                Low = low,
                Volume = volume,
                Sparkline = sparkline
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Yahoo Finance chart response");
            return null;
        }
    }

    private static decimal GetDecimal(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDecimal();
        return 0;
    }

    private static long GetLong(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt64();
        return 0;
    }
}
