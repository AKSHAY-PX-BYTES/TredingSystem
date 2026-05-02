using System.Text.Json;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IMarketDataService
{
    Task<StockQuote> GetQuoteAsync(string symbol);
    Task<List<StockData>> GetHistoricalDataAsync(string symbol, int days = 90);
    Task<TechnicalIndicators> GetIndicatorsAsync(string symbol);
}

public class MarketDataService : IMarketDataService
{
    private readonly ILogger<MarketDataService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private static readonly Dictionary<string, (DateTime Ts, StockQuote Data)> _quoteCache = new();
    private static readonly Dictionary<string, (DateTime Ts, List<StockData> Data)> _historyCache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    // Fallback company names for known symbols
    private static readonly Dictionary<string, string> _companyNames = new()
    {
        ["AAPL"] = "Apple Inc.", ["MSFT"] = "Microsoft Corp.", ["GOOGL"] = "Alphabet Inc.",
        ["AMZN"] = "Amazon.com Inc.", ["TSLA"] = "Tesla Inc.", ["NVDA"] = "NVIDIA Corp.",
        ["META"] = "Meta Platforms Inc.", ["JPM"] = "JPMorgan Chase & Co.",
        ["V"] = "Visa Inc.", ["WMT"] = "Walmart Inc.", ["JNJ"] = "Johnson & Johnson",
        ["PG"] = "Procter & Gamble Co.", ["MA"] = "Mastercard Inc.",
        ["UNH"] = "UnitedHealth Group Inc.", ["HD"] = "Home Depot Inc.",
        ["DIS"] = "Walt Disney Co.", ["NFLX"] = "Netflix Inc.", ["AMD"] = "Advanced Micro Devices",
        ["CRM"] = "Salesforce Inc.", ["INTC"] = "Intel Corp.", ["BA"] = "Boeing Co.",
        ["RELIANCE"] = "Reliance Industries Ltd", ["TCS"] = "Tata Consultancy Services",
        ["INFY"] = "Infosys Ltd", ["HDFCBANK"] = "HDFC Bank Ltd",
        ["ICICIBANK"] = "ICICI Bank Ltd", ["SBIN"] = "State Bank of India",
        ["BHARTIARTL"] = "Bharti Airtel Ltd", ["ITC"] = "ITC Ltd",
        ["KOTAKBANK"] = "Kotak Mahindra Bank", ["WIPRO"] = "Wipro Ltd",
        ["TATAMOTORS"] = "Tata Motors Ltd", ["SUNPHARMA"] = "Sun Pharmaceutical",
        ["MARUTI"] = "Maruti Suzuki India", ["LT"] = "Larsen & Toubro",
        ["AXISBANK"] = "Axis Bank Ltd", ["BAJFINANCE"] = "Bajaj Finance Ltd",
        ["HCLTECH"] = "HCL Technologies", ["HINDUNILVR"] = "Hindustan Unilever",
    };

    public MarketDataService(ILogger<MarketDataService> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<StockQuote> GetQuoteAsync(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        _logger.LogInformation("Fetching live quote for {Symbol}", symbol);

        // Check quote cache (30 seconds)
        if (_quoteCache.TryGetValue(symbol, out var cached) &&
            (DateTime.UtcNow - cached.Ts).TotalSeconds < 30)
            return cached.Data;

        try
        {
            var yahooSymbol = MapToYahooSymbol(symbol);
            var client = _httpFactory.CreateClient("YahooFinance");

            // Fetch 1d chart with 5m intervals for intraday + 30d for historical
            var chartUrl = $"v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?interval=1d&range=3mo&includePrePost=false";
            using var response = await client.GetAsync(chartUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var quote = ParseYahooChartToQuote(json, symbol);

                if (quote != null)
                {
                    _quoteCache[symbol] = (DateTime.UtcNow, quote);
                    _logger.LogInformation("Live quote for {Symbol}: {Price}", symbol, quote.CurrentPrice);
                    return quote;
                }
            }
            else
            {
                _logger.LogWarning("Yahoo returned {Code} for {Symbol}", (int)response.StatusCode, symbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live data for {Symbol}, using fallback", symbol);
        }

        // Fallback to simulated data
        return GenerateFallbackQuote(symbol);
    }

    public async Task<List<StockData>> GetHistoricalDataAsync(string symbol, int days = 90)
    {
        symbol = symbol.ToUpperInvariant();
        _logger.LogInformation("Fetching {Days} days of historical data for {Symbol}", days, symbol);

        // Check history cache (5 minutes)
        var cacheKey = $"{symbol}_{days}";
        if (_historyCache.TryGetValue(cacheKey, out var cached) &&
            (DateTime.UtcNow - cached.Ts).TotalMinutes < 5)
            return cached.Data;

        try
        {
            var yahooSymbol = MapToYahooSymbol(symbol);
            var range = days <= 30 ? "1mo" : days <= 90 ? "3mo" : days <= 180 ? "6mo" : "1y";
            var client = _httpFactory.CreateClient("YahooFinance");
            var url = $"v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?interval=1d&range={range}&includePrePost=false";

            using var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = ParseYahooChartToHistory(json, symbol);

                if (data != null && data.Count > 0)
                {
                    _historyCache[cacheKey] = (DateTime.UtcNow, data);
                    _logger.LogInformation("Fetched {Count} days of live history for {Symbol}", data.Count, symbol);
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live history for {Symbol}, using fallback", symbol);
        }

        // Fallback
        return GenerateFallbackHistory(symbol, days);
    }

    public async Task<TechnicalIndicators> GetIndicatorsAsync(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        _logger.LogInformation("Calculating indicators for {Symbol}", symbol);

        var historicalData = await GetHistoricalDataAsync(symbol, 250);
        var closes = historicalData.Select(d => d.Close).ToList();

        var indicators = new TechnicalIndicators
        {
            Symbol = symbol,
            CurrentPrice = closes.Last(),
            SMA20 = CalculateSMA(closes, 20),
            SMA50 = CalculateSMA(closes, 50),
            SMA200 = CalculateSMA(closes, 200),
            EMA12 = CalculateEMA(closes, 12),
            EMA26 = CalculateEMA(closes, 26),
            RSI = CalculateRSI(closes, 14),
        };

        // Bollinger Bands (20-period, 2 std dev)
        var sma20 = indicators.SMA20;
        var stdDev = CalculateStdDev(closes.TakeLast(20).ToList());
        indicators.BollingerMiddle = sma20;
        indicators.BollingerUpper = sma20 + (2 * stdDev);
        indicators.BollingerLower = sma20 - (2 * stdDev);

        indicators.CalculatedAt = DateTime.UtcNow;

        return indicators;
    }

    // ── Yahoo Finance parsing ───────────────────────────────────

    private StockQuote? ParseYahooChartToQuote(string json, string symbol)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement
                .GetProperty("chart").GetProperty("result");
            if (result.GetArrayLength() == 0) return null;

            var r = result[0];
            var meta = r.GetProperty("meta");

            var price = GetDecimal(meta, "regularMarketPrice");
            if (price == 0) return null;

            var prevClose = GetDecimal(meta, "chartPreviousClose");
            if (prevClose == 0) prevClose = GetDecimal(meta, "previousClose");
            if (prevClose == 0) prevClose = price;

            var high = GetDecimal(meta, "regularMarketDayHigh");
            var low = GetDecimal(meta, "regularMarketDayLow");
            var volume = GetLong(meta, "regularMarketVolume");
            var name = _companyNames.GetValueOrDefault(symbol, symbol);

            // Extract the native currency from Yahoo (e.g. "USD", "INR", "GBP")
            var priceCurrency = "USD";
            if (meta.TryGetProperty("currency", out var currProp) && currProp.ValueKind == JsonValueKind.String)
                priceCurrency = currProp.GetString() ?? "USD";

            // Parse historical data for chart
            var history = ParseYahooChartToHistory(json, symbol) ?? new();

            return new StockQuote
            {
                Symbol = symbol,
                CompanyName = name,
                CurrentPrice = price,
                PreviousClose = prevClose,
                DayHigh = high > 0 ? high : price,
                DayLow = low > 0 ? low : price,
                Volume = volume,
                PriceCurrency = priceCurrency,
                LastUpdated = DateTime.UtcNow,
                HistoricalData = history.TakeLast(30).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Yahoo chart for {Symbol}", symbol);
            return null;
        }
    }

    private List<StockData>? ParseYahooChartToHistory(string json, string symbol)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result");
            if (result.GetArrayLength() == 0) return null;

            var r = result[0];
            if (!r.TryGetProperty("timestamp", out var timestamps)) return null;
            if (!r.TryGetProperty("indicators", out var indicators)) return null;
            var quote = indicators.GetProperty("quote")[0];

            var opens = quote.GetProperty("open");
            var highs = quote.GetProperty("high");
            var lows = quote.GetProperty("low");
            var closes = quote.GetProperty("close");
            var volumes = quote.GetProperty("volume");

            var data = new List<StockData>();
            var len = timestamps.GetArrayLength();

            for (int i = 0; i < len; i++)
            {
                var closeVal = GetArrayDecimal(closes, i);
                if (closeVal == 0) continue; // skip null entries

                var openVal = GetArrayDecimal(opens, i);
                var highVal = GetArrayDecimal(highs, i);
                var lowVal = GetArrayDecimal(lows, i);
                var vol = GetArrayLong(volumes, i);
                var ts = timestamps[i].GetInt64();
                var date = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;

                var prevClose = data.Count > 0 ? data[^1].Close : closeVal;

                data.Add(new StockData
                {
                    Symbol = symbol,
                    Date = date,
                    Open = Math.Round(openVal, 2),
                    High = Math.Round(highVal, 2),
                    Low = Math.Round(lowVal, 2),
                    Close = Math.Round(closeVal, 2),
                    Volume = vol
                });
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Yahoo history for {Symbol}", symbol);
            return null;
        }
    }

    // ── Yahoo symbol mapping ────────────────────────────────────

    private static string MapToYahooSymbol(string symbol)
    {
        // Indian stocks — try NSE first
        if (IsIndianStock(symbol))
            return $"{symbol}.NS";

        // Already has suffix
        if (symbol.Contains('.')) return symbol;

        // Fix BRK.B
        if (symbol == "BRK") return "BRK-B";

        // US stocks (default)
        return symbol;
    }

    private static bool IsIndianStock(string symbol)
    {
        return symbol is "RELIANCE" or "TCS" or "INFY" or "HDFCBANK" or "ICICIBANK" or "SBIN"
            or "BHARTIARTL" or "ITC" or "KOTAKBANK" or "WIPRO" or "TATAMOTORS"
            or "SUNPHARMA" or "MARUTI" or "LT" or "AXISBANK" or "BAJFINANCE"
            or "HCLTECH" or "HINDUNILVR" or "TITAN" or "ASIANPAINT";
    }

    // ── JSON helpers ────────────────────────────────────────────

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

    private static decimal GetArrayDecimal(JsonElement arr, int index)
    {
        var el = arr[index];
        return el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : 0;
    }

    private static long GetArrayLong(JsonElement arr, int index)
    {
        var el = arr[index];
        return el.ValueKind == JsonValueKind.Number ? el.GetInt64() : 0;
    }

    // ── Fallback (simulated) data ───────────────────────────────

    private StockQuote GenerateFallbackQuote(string symbol)
    {
        var name = symbol;
        if (_companyNames.TryGetValue(symbol, out var n)) name = n;

        var history = GenerateFallbackHistory(symbol, 90);
        var latest = history.Last();
        var previous = history[^2];

        return new StockQuote
        {
            Symbol = symbol,
            CompanyName = name,
            CurrentPrice = latest.Close,
            PreviousClose = previous.Close,
            DayHigh = latest.High,
            DayLow = latest.Low,
            Volume = latest.Volume,
            PriceCurrency = IsIndianStock(symbol) ? "INR" : "USD",
            LastUpdated = DateTime.UtcNow,
            HistoricalData = history.TakeLast(30).ToList()
        };
    }

    private static readonly Dictionary<string, List<StockData>> _fallbackCache = new();

    private List<StockData> GenerateFallbackHistory(string symbol, int days)
    {
        var cacheKey = $"fallback_{symbol}_{days}";
        if (_fallbackCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var rng = new Random(symbol.GetHashCode());
        var basePrice = _companyNames.ContainsKey(symbol) ? 150m : 100m;
        var price = basePrice;
        var data = new List<StockData>();

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

            var dailyReturn = (decimal)(rng.NextDouble() * 0.06 - 0.03);
            price *= (1 + dailyReturn);
            var high = price * (1 + (decimal)(rng.NextDouble() * 0.02));
            var low = price * (1 - (decimal)(rng.NextDouble() * 0.02));
            var open = low + (high - low) * (decimal)rng.NextDouble();

            data.Add(new StockData
            {
                Symbol = symbol, Date = date,
                Open = Math.Round(open, 2), High = Math.Round(high, 2),
                Low = Math.Round(low, 2), Close = Math.Round(price, 2),
                Volume = rng.Next(5_000_000, 50_000_000)
            });
        }

        _fallbackCache[cacheKey] = data;
        return data;
    }

    // ── Technical Indicator Calculations (unchanged) ────────────

    public static decimal CalculateSMA(List<decimal> data, int period)
    {
        if (data.Count < period) return data.LastOrDefault();
        return Math.Round(data.TakeLast(period).Average(), 2);
    }

    public static decimal CalculateEMA(List<decimal> data, int period)
    {
        if (data.Count < period) return data.LastOrDefault();
        var multiplier = 2m / (period + 1);
        var ema = data.Take(period).Average();
        foreach (var price in data.Skip(period))
        {
            ema = (price - ema) * multiplier + ema;
        }
        return Math.Round(ema, 2);
    }

    public static decimal CalculateRSI(List<decimal> data, int period = 14)
    {
        if (data.Count < period + 1) return 50m;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < data.Count; i++)
        {
            var change = data[i] - data[i - 1];
            if (change >= 0) { gains.Add(change); losses.Add(0); }
            else { gains.Add(0); losses.Add(Math.Abs(change)); }
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100m;
        var rs = avgGain / avgLoss;
        return Math.Round(100m - (100m / (1m + rs)), 2);
    }

    private static decimal CalculateStdDev(List<decimal> data)
    {
        if (data.Count == 0) return 0;
        var avg = data.Average();
        var sumOfSquares = data.Sum(d => (d - avg) * (d - avg));
        return Math.Round((decimal)Math.Sqrt((double)(sumOfSquares / data.Count)), 2);
    }
}
