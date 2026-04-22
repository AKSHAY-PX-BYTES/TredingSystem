using System.Collections.Concurrent;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IMarketExchangeService
{
    Task<MarketOverview> GetMarketOverviewAsync();
    Task<ExchangeData> GetExchangeDataAsync(string exchange);
    List<string> GetSupportedExchanges();
}

public class MarketExchangeService : IMarketExchangeService
{
    private readonly ILiveMarketDataService _liveData;
    private readonly ILogger<MarketExchangeService> _logger;
    private static readonly ConcurrentDictionary<string, (DateTime Ts, ExchangeData Data)> _liveCache = new();

    public MarketExchangeService(ILiveMarketDataService liveData, ILogger<MarketExchangeService> logger)
    {
        _liveData = liveData;
        _logger = logger;
    }

    private static readonly Random _rng = new(123);
    private static readonly object _lock = new();
    private static DateTime _lastGenerated = DateTime.MinValue;
    private static Dictionary<string, ExchangeData>? _cachedData;

    // ── NSE Stocks ──────────────────────────────────────────────
    private static readonly List<(string Symbol, string Name, string Sector, decimal BasePrice, decimal MCap)> _nseStocks = new()
    {
        ("RELIANCE", "Reliance Industries", "Energy", 2450.50m, 16600000m),
        ("TCS", "Tata Consultancy Services", "IT", 3820.30m, 13900000m),
        ("HDFCBANK", "HDFC Bank Ltd", "Banking", 1620.75m, 12300000m),
        ("INFY", "Infosys Ltd", "IT", 1485.60m, 6200000m),
        ("ICICIBANK", "ICICI Bank Ltd", "Banking", 1075.40m, 7500000m),
        ("HINDUNILVR", "Hindustan Unilever", "FMCG", 2380.90m, 5600000m),
        ("SBIN", "State Bank of India", "Banking", 628.50m, 5600000m),
        ("BHARTIARTL", "Bharti Airtel Ltd", "Telecom", 1520.80m, 8500000m),
        ("KOTAKBANK", "Kotak Mahindra Bank", "Banking", 1780.20m, 3500000m),
        ("ITC", "ITC Ltd", "FMCG", 438.60m, 5500000m),
        ("LT", "Larsen & Toubro", "Infrastructure", 3450.70m, 4700000m),
        ("AXISBANK", "Axis Bank Ltd", "Banking", 1125.30m, 3500000m),
        ("WIPRO", "Wipro Ltd", "IT", 465.80m, 2400000m),
        ("BAJFINANCE", "Bajaj Finance Ltd", "Finance", 6850.40m, 4200000m),
        ("MARUTI", "Maruti Suzuki India", "Auto", 10850.60m, 3300000m),
        ("SUNPHARMA", "Sun Pharmaceutical", "Pharma", 1180.30m, 2800000m),
        ("TATAMOTORS", "Tata Motors Ltd", "Auto", 685.90m, 2500000m),
        ("TITAN", "Titan Company Ltd", "Consumer", 3250.40m, 2900000m),
        ("ASIANPAINT", "Asian Paints Ltd", "Consumer", 2780.50m, 2700000m),
        ("HCLTECH", "HCL Technologies", "IT", 1380.20m, 3700000m),
    };

    // ── BSE Stocks ──────────────────────────────────────────────
    private static readonly List<(string Symbol, string Name, string Sector, decimal BasePrice, decimal MCap)> _bseStocks = new()
    {
        ("RELIANCE.BO", "Reliance Industries", "Energy", 2452.30m, 16600000m),
        ("TCS.BO", "TCS Ltd", "IT", 3818.50m, 13900000m),
        ("HDFCBANK.BO", "HDFC Bank", "Banking", 1618.90m, 12300000m),
        ("INFY.BO", "Infosys", "IT", 1483.70m, 6200000m),
        ("ICICIBANK.BO", "ICICI Bank", "Banking", 1073.80m, 7500000m),
        ("HINDUNILVR.BO", "Hindustan Unilever", "FMCG", 2378.40m, 5600000m),
        ("SBIN.BO", "State Bank of India", "Banking", 627.10m, 5600000m),
        ("BHARTIARTL.BO", "Bharti Airtel", "Telecom", 1518.60m, 8500000m),
        ("ADANIENT.BO", "Adani Enterprises", "Conglomerate", 2680.50m, 3100000m),
        ("ADANIPORTS.BO", "Adani Ports & SEZ", "Infrastructure", 1180.30m, 2500000m),
        ("BAJAJ-AUTO.BO", "Bajaj Auto Ltd", "Auto", 7250.80m, 2200000m),
        ("POWERGRID.BO", "Power Grid Corp", "Power", 285.40m, 2600000m),
        ("NTPC.BO", "NTPC Ltd", "Power", 345.60m, 3400000m),
        ("ONGC.BO", "ONGC Ltd", "Energy", 265.80m, 3300000m),
        ("COALINDIA.BO", "Coal India Ltd", "Mining", 428.90m, 2600000m),
        ("TATASTEEL.BO", "Tata Steel Ltd", "Metals", 142.50m, 1800000m),
        ("JSWSTEEL.BO", "JSW Steel Ltd", "Metals", 825.30m, 2000000m),
        ("BPCL.BO", "Bharat Petroleum", "Energy", 580.60m, 1300000m),
        ("GRASIM.BO", "Grasim Industries", "Cement", 2150.40m, 1400000m),
        ("DRREDDY.BO", "Dr Reddy's Labs", "Pharma", 5680.20m, 950000m),
    };

    // ── US Stocks (NYSE + NASDAQ) ───────────────────────────────
    private static readonly List<(string Symbol, string Name, string Sector, decimal BasePrice, decimal MCap, Exchange Ex)> _usStocks = new()
    {
        ("AAPL", "Apple Inc.", "Technology", 178.50m, 2800000m, Exchange.NASDAQ),
        ("MSFT", "Microsoft Corp.", "Technology", 415.20m, 3100000m, Exchange.NASDAQ),
        ("GOOGL", "Alphabet Inc.", "Technology", 175.80m, 2200000m, Exchange.NASDAQ),
        ("AMZN", "Amazon.com Inc.", "Consumer", 185.60m, 1900000m, Exchange.NASDAQ),
        ("NVDA", "NVIDIA Corp.", "Semiconductors", 875.40m, 2200000m, Exchange.NASDAQ),
        ("TSLA", "Tesla Inc.", "Auto", 245.30m, 780000m, Exchange.NASDAQ),
        ("META", "Meta Platforms", "Technology", 505.75m, 1300000m, Exchange.NASDAQ),
        ("BRK.B", "Berkshire Hathaway", "Finance", 408.50m, 890000m, Exchange.NYSE),
        ("JPM", "JPMorgan Chase", "Banking", 198.40m, 570000m, Exchange.NYSE),
        ("V", "Visa Inc.", "Finance", 280.90m, 510000m, Exchange.NYSE),
        ("JNJ", "Johnson & Johnson", "Healthcare", 156.80m, 380000m, Exchange.NYSE),
        ("WMT", "Walmart Inc.", "Retail", 165.20m, 445000m, Exchange.NYSE),
        ("PG", "Procter & Gamble", "Consumer", 158.30m, 370000m, Exchange.NYSE),
        ("MA", "Mastercard Inc.", "Finance", 460.70m, 430000m, Exchange.NYSE),
        ("UNH", "UnitedHealth Group", "Healthcare", 528.40m, 490000m, Exchange.NYSE),
        ("HD", "Home Depot Inc.", "Retail", 345.60m, 340000m, Exchange.NYSE),
        ("DIS", "Walt Disney Co.", "Entertainment", 112.30m, 205000m, Exchange.NYSE),
        ("NFLX", "Netflix Inc.", "Entertainment", 628.50m, 275000m, Exchange.NASDAQ),
        ("AMD", "Advanced Micro Devices", "Semiconductors", 178.90m, 290000m, Exchange.NASDAQ),
        ("CRM", "Salesforce Inc.", "Technology", 298.40m, 290000m, Exchange.NYSE),
    };

    // ── Global Stocks (LSE / TSE) ───────────────────────────────
    private static readonly List<(string Symbol, string Name, string Sector, decimal BasePrice, decimal MCap, string Exchange)> _globalStocks = new()
    {
        ("SHEL.L", "Shell PLC", "Energy", 2680.50m, 210000m, "LSE"),
        ("AZN.L", "AstraZeneca PLC", "Pharma", 11250.00m, 220000m, "LSE"),
        ("HSBA.L", "HSBC Holdings", "Banking", 685.40m, 130000m, "LSE"),
        ("ULVR.L", "Unilever PLC", "FMCG", 4320.00m, 110000m, "LSE"),
        ("BP.L", "BP PLC", "Energy", 528.60m, 95000m, "LSE"),
        ("GSK.L", "GSK PLC", "Pharma", 1580.00m, 80000m, "LSE"),
        ("RIO.L", "Rio Tinto PLC", "Mining", 5480.00m, 90000m, "LSE"),
        ("BARC.L", "Barclays PLC", "Banking", 195.80m, 32000m, "LSE"),
        ("VOD.L", "Vodafone Group", "Telecom", 72.50m, 20000m, "LSE"),
        ("LLOY.L", "Lloyds Banking", "Banking", 52.80m, 35000m, "LSE"),
        ("7203.T", "Toyota Motor Corp", "Auto", 2850.00m, 350000m, "TSE"),
        ("6758.T", "Sony Group Corp", "Technology", 13250.00m, 170000m, "TSE"),
        ("9984.T", "SoftBank Group", "Technology", 8450.00m, 110000m, "TSE"),
        ("6861.T", "Keyence Corp", "Electronics", 62500.00m, 150000m, "TSE"),
        ("9432.T", "NTT Corp", "Telecom", 172.50m, 140000m, "TSE"),
        ("8306.T", "Mitsubishi UFJ", "Banking", 1350.00m, 120000m, "TSE"),
        ("6902.T", "Denso Corp", "Auto Parts", 2180.00m, 65000m, "TSE"),
        ("4502.T", "Takeda Pharma", "Pharma", 4150.00m, 65000m, "TSE"),
        ("7267.T", "Honda Motor Co", "Auto", 1680.00m, 85000m, "TSE"),
        ("8035.T", "Tokyo Electron", "Semiconductors", 28500.00m, 130000m, "TSE"),
    };

    // ── Index definitions ───────────────────────────────────────
    private static readonly List<(string Name, string Symbol, Exchange Exchange, decimal BaseValue)> _indices = new()
    {
        ("NIFTY 50", "NIFTY50", Exchange.NSE, 22450.80m),
        ("NIFTY Bank", "BANKNIFTY", Exchange.NSE, 47850.30m),
        ("NIFTY IT", "NIFTYIT", Exchange.NSE, 35680.50m),
        ("NIFTY Pharma", "NIFTYPHARMA", Exchange.NSE, 18250.40m),
        ("SENSEX", "SENSEX", Exchange.BSE, 73850.60m),
        ("BSE MidCap", "BSEMIDCAP", Exchange.BSE, 38450.20m),
        ("BSE SmallCap", "BSESMALLCAP", Exchange.BSE, 42680.70m),
        ("S&P 500", "SPX", Exchange.NYSE, 5250.40m),
        ("Dow Jones", "DJI", Exchange.NYSE, 38850.20m),
        ("NASDAQ Comp", "IXIC", Exchange.NASDAQ, 16480.70m),
        ("Russell 2000", "RUT", Exchange.NYSE, 2050.30m),
        ("FTSE 100", "FTSE", Exchange.LSE, 8125.40m),
        ("Nikkei 225", "N225", Exchange.TSE, 38450.60m),
    };

    // ── Yahoo Finance symbol mappings ───────────────────────────
    private static readonly Dictionary<string, string> _yahooIndexMap = new()
    {
        ["NIFTY50"] = "^NSEI", ["BANKNIFTY"] = "^NSEBANK", ["NIFTYIT"] = "^CNXIT",
        ["NIFTYPHARMA"] = "^CNXPHARMA", ["SENSEX"] = "^BSESN",
        ["SPX"] = "^GSPC", ["DJI"] = "^DJI", ["IXIC"] = "^IXIC", ["RUT"] = "^RUT",
        ["FTSE"] = "^FTSE", ["N225"] = "^N225"
    };

    private static string ToYahooSymbol(string symbol, string exchange) => exchange switch
    {
        "NSE" => $"{symbol}.NS",
        // BSE (.BO), LSE (.L), TSE (.T) symbols already match Yahoo format
        _ => symbol.Replace("BRK.B", "BRK-B")
    };

    public List<string> GetSupportedExchanges() => new() { "NSE", "BSE", "US", "GLOBAL" };

    public Task<MarketOverview> GetMarketOverviewAsync()
    {
        EnsureDataGenerated();

        var overview = new MarketOverview
        {
            Indices = _cachedData!.Values.SelectMany(e => e.Indices).ToList(),
            ExchangeStocks = _cachedData!.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.AllStocks.Take(5).ToList()
            ),
            LastUpdated = DateTime.UtcNow
        };

        return Task.FromResult(overview);
    }

    public async Task<ExchangeData> GetExchangeDataAsync(string exchange)
    {
        var key = exchange.ToUpperInvariant();

        // Return live-cached data if still fresh (30s)
        if (_liveCache.TryGetValue(key, out var lc) && (DateTime.UtcNow - lc.Ts).TotalSeconds < 30)
            return lc.Data;

        // Get base simulated data as structure/fallback
        EnsureDataGenerated();
        if (!_cachedData!.TryGetValue(key, out var baseData))
            return new ExchangeData { ExchangeName = exchange, ExchangeCode = exchange, Status = "Unknown" };

        // Enrich with real live market data from Yahoo Finance
        var enriched = await EnrichWithLiveDataAsync(baseData, key);
        _liveCache[key] = (DateTime.UtcNow, enriched);
        return enriched;
    }

    private static void EnsureDataGenerated()
    {
        lock (_lock)
        {
            // Regenerate simulated fallback data every 10 minutes
            if (_cachedData != null && (DateTime.UtcNow - _lastGenerated).TotalMinutes < 10)
                return;

            _cachedData = new Dictionary<string, ExchangeData>
            {
                ["NSE"] = GenerateNSEData(),
                ["BSE"] = GenerateBSEData(),
                ["US"] = GenerateUSData(),
                ["GLOBAL"] = GenerateGlobalData()
            };
            _lastGenerated = DateTime.UtcNow;
        }
    }

    private static ExchangeData GenerateNSEData()
    {
        var rng = new Random((int)DateTime.UtcNow.Ticks ^ 1);
        var stocks = _nseStocks.Select(s => GenerateExchangeStock(s.Symbol, s.Name, s.Sector, s.BasePrice, s.MCap, Exchange.NSE, rng)).ToList();
        var indices = _indices.Where(i => i.Exchange == Exchange.NSE)
            .Select(i => GenerateIndex(i.Name, i.Symbol, i.Exchange, i.BaseValue, rng)).ToList();

        return new ExchangeData
        {
            ExchangeName = "National Stock Exchange",
            ExchangeCode = "NSE",
            Country = "India",
            Flag = "🇮🇳",
            Currency = "INR",
            Status = GetMarketStatus("Asia/Kolkata", 9, 15, 15, 30),
            Timezone = "IST (UTC+5:30)",
            Indices = indices,
            TopGainers = stocks.OrderByDescending(s => s.ChangePercent).Take(5).ToList(),
            TopLosers = stocks.OrderBy(s => s.ChangePercent).Take(5).ToList(),
            MostActive = stocks.OrderByDescending(s => s.Volume).Take(5).ToList(),
            AllStocks = stocks
        };
    }

    private static ExchangeData GenerateBSEData()
    {
        var rng = new Random((int)DateTime.UtcNow.Ticks ^ 2);
        var stocks = _bseStocks.Select(s => GenerateExchangeStock(s.Symbol, s.Name, s.Sector, s.BasePrice, s.MCap, Exchange.BSE, rng)).ToList();
        var indices = _indices.Where(i => i.Exchange == Exchange.BSE)
            .Select(i => GenerateIndex(i.Name, i.Symbol, i.Exchange, i.BaseValue, rng)).ToList();

        return new ExchangeData
        {
            ExchangeName = "Bombay Stock Exchange",
            ExchangeCode = "BSE",
            Country = "India",
            Flag = "🇮🇳",
            Currency = "INR",
            Status = GetMarketStatus("Asia/Kolkata", 9, 15, 15, 30),
            Timezone = "IST (UTC+5:30)",
            Indices = indices,
            TopGainers = stocks.OrderByDescending(s => s.ChangePercent).Take(5).ToList(),
            TopLosers = stocks.OrderBy(s => s.ChangePercent).Take(5).ToList(),
            MostActive = stocks.OrderByDescending(s => s.Volume).Take(5).ToList(),
            AllStocks = stocks
        };
    }

    private static ExchangeData GenerateUSData()
    {
        var rng = new Random((int)DateTime.UtcNow.Ticks ^ 3);
        var stocks = _usStocks.Select(s => GenerateExchangeStock(s.Symbol, s.Name, s.Sector, s.BasePrice, s.MCap, s.Ex, rng)).ToList();
        var indices = _indices.Where(i => i.Exchange == Exchange.NYSE || i.Exchange == Exchange.NASDAQ)
            .Select(i => GenerateIndex(i.Name, i.Symbol, i.Exchange, i.BaseValue, rng)).ToList();

        return new ExchangeData
        {
            ExchangeName = "US Markets (NYSE + NASDAQ)",
            ExchangeCode = "US",
            Country = "United States",
            Flag = "🇺🇸",
            Currency = "USD",
            Status = GetMarketStatus("America/New_York", 9, 30, 16, 0),
            Timezone = "ET (UTC-4)",
            Indices = indices,
            TopGainers = stocks.OrderByDescending(s => s.ChangePercent).Take(5).ToList(),
            TopLosers = stocks.OrderBy(s => s.ChangePercent).Take(5).ToList(),
            MostActive = stocks.OrderByDescending(s => s.Volume).Take(5).ToList(),
            AllStocks = stocks
        };
    }

    private static ExchangeData GenerateGlobalData()
    {
        var rng = new Random((int)DateTime.UtcNow.Ticks ^ 4);
        var stocks = _globalStocks.Select(s =>
        {
            var ex = s.Exchange == "LSE" ? Exchange.LSE : Exchange.TSE;
            return GenerateExchangeStock(s.Symbol, s.Name, s.Sector, s.BasePrice, s.MCap, ex, rng);
        }).ToList();

        var indices = _indices.Where(i => i.Exchange == Exchange.LSE || i.Exchange == Exchange.TSE)
            .Select(i => GenerateIndex(i.Name, i.Symbol, i.Exchange, i.BaseValue, rng)).ToList();

        return new ExchangeData
        {
            ExchangeName = "Global Markets (LSE + TSE)",
            ExchangeCode = "GLOBAL",
            Country = "UK / Japan",
            Flag = "🌍",
            Currency = "Multi",
            Status = "Open",
            Timezone = "Multiple",
            Indices = indices,
            TopGainers = stocks.OrderByDescending(s => s.ChangePercent).Take(5).ToList(),
            TopLosers = stocks.OrderBy(s => s.ChangePercent).Take(5).ToList(),
            MostActive = stocks.OrderByDescending(s => s.Volume).Take(5).ToList(),
            AllStocks = stocks
        };
    }

    private static ExchangeStock GenerateExchangeStock(string symbol, string name, string sector, decimal basePrice, decimal mCap, Exchange exchange, Random rng)
    {
        var changePercent = (decimal)(rng.NextDouble() * 8 - 4); // -4% to +4%
        var prevClose = basePrice;
        var currentPrice = Math.Round(prevClose * (1 + changePercent / 100), 2);

        // Generate 20-point sparkline price history (intraday simulation)
        var history = new List<decimal>();
        var p = prevClose;
        for (int i = 0; i < 20; i++)
        {
            var drift = (currentPrice - prevClose) / 20;
            var noise = (decimal)(rng.NextDouble() * (double)(basePrice * 0.005m)) - basePrice * 0.0025m;
            p = Math.Round(p + drift + noise, 2);
            history.Add(p);
        }
        history[^1] = currentPrice; // ensure last point matches current

        var dayHigh = Math.Max(currentPrice, history.Max()) + Math.Round((decimal)(rng.NextDouble() * (double)(basePrice * 0.002m)), 2);
        var dayLow = Math.Min(currentPrice, history.Min()) - Math.Round((decimal)(rng.NextDouble() * (double)(basePrice * 0.002m)), 2);

        return new ExchangeStock
        {
            Symbol = symbol,
            CompanyName = name,
            Exchange = exchange,
            Sector = sector,
            CurrentPrice = currentPrice,
            PreviousClose = prevClose,
            DayHigh = dayHigh,
            DayLow = dayLow,
            Volume = rng.Next(500_000, 80_000_000),
            MarketCap = mCap,
            PriceHistory = history
        };
    }

    private static MarketIndex GenerateIndex(string name, string symbol, Exchange exchange, decimal baseValue, Random rng)
    {
        var changePercent = (decimal)(rng.NextDouble() * 4 - 2); // -2% to +2%
        var prevClose = baseValue;
        var currentValue = Math.Round(prevClose * (1 + changePercent / 100), 2);

        return new MarketIndex
        {
            Name = name,
            Symbol = symbol,
            Exchange = exchange,
            CurrentValue = currentValue,
            PreviousClose = prevClose,
            LastUpdated = DateTime.UtcNow,
            Status = "Open"
        };
    }

    // ── Live Data Enrichment (Yahoo Finance) ────────────────────
    private async Task<ExchangeData> EnrichWithLiveDataAsync(ExchangeData baseData, string exchange)
    {
        try
        {
            // Build Yahoo symbol maps for stocks and indices
            var stockYahooMap = baseData.AllStocks.ToDictionary(
                s => ToYahooSymbol(s.Symbol, exchange),
                s => s
            );

            var indexYahooMap = baseData.Indices
                .Where(i => _yahooIndexMap.ContainsKey(i.Symbol) && _yahooIndexMap[i.Symbol] != "")
                .ToDictionary(
                    i => _yahooIndexMap[i.Symbol],
                    i => i
                );

            // Fetch all live quotes in parallel (throttled)
            var allYahooSymbols = stockYahooMap.Keys.Concat(indexYahooMap.Keys);
            var quotes = await _liveData.FetchBatchAsync(allYahooSymbols);

            if (quotes.Count == 0)
            {
                _logger.LogWarning("No live data for {Exchange}, using simulated fallback", exchange);
                return baseData;
            }

            int liveStockCount = 0;

            // Enrich stocks with live prices
            var enrichedStocks = baseData.AllStocks.Select(stock =>
            {
                var yahoo = ToYahooSymbol(stock.Symbol, exchange);
                if (quotes.TryGetValue(yahoo, out var live))
                {
                    liveStockCount++;
                    return new ExchangeStock
                    {
                        Symbol = stock.Symbol,
                        CompanyName = stock.CompanyName,
                        Exchange = stock.Exchange,
                        Sector = stock.Sector,
                        CurrentPrice = live.Price,
                        PreviousClose = live.PreviousClose,
                        DayHigh = live.High > 0 ? live.High : stock.DayHigh,
                        DayLow = live.Low > 0 ? live.Low : stock.DayLow,
                        Volume = live.Volume > 0 ? live.Volume : stock.Volume,
                        MarketCap = stock.MarketCap, // Keep static market cap
                        PriceHistory = live.Sparkline.Count > 1 ? live.Sparkline : stock.PriceHistory
                    };
                }
                return stock; // Fallback to simulated
            }).ToList();

            // Enrich indices with live values
            var enrichedIndices = baseData.Indices.Select(idx =>
            {
                var yahoo = _yahooIndexMap.GetValueOrDefault(idx.Symbol, "");
                if (yahoo != "" && quotes.TryGetValue(yahoo, out var live))
                {
                    return new MarketIndex
                    {
                        Name = idx.Name,
                        Symbol = idx.Symbol,
                        Exchange = idx.Exchange,
                        CurrentValue = live.Price,
                        PreviousClose = live.PreviousClose,
                        LastUpdated = DateTime.UtcNow,
                        Status = idx.Status
                    };
                }
                return idx;
            }).ToList();

            _logger.LogInformation("{Exchange}: {LiveCount}/{Total} stocks with live prices",
                exchange, liveStockCount, enrichedStocks.Count);

            return new ExchangeData
            {
                ExchangeName = baseData.ExchangeName,
                ExchangeCode = baseData.ExchangeCode,
                Country = baseData.Country,
                Flag = baseData.Flag,
                Currency = baseData.Currency,
                Status = baseData.Status,
                Timezone = baseData.Timezone,
                Indices = enrichedIndices,
                AllStocks = enrichedStocks,
                TopGainers = enrichedStocks.OrderByDescending(s => s.ChangePercent).Take(5).ToList(),
                TopLosers = enrichedStocks.OrderBy(s => s.ChangePercent).Take(5).ToList(),
                MostActive = enrichedStocks.OrderByDescending(s => s.Volume).Take(5).ToList(),
                IsLive = liveStockCount > 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live data enrichment failed for {Exchange}, using simulated", exchange);
            return baseData;
        }
    }

    private static string GetMarketStatus(string _timezone, int openHour, int openMin, int closeHour, int closeMin)
    {
        // Simplified — use UTC approximation
        var utcNow = DateTime.UtcNow;
        var hour = utcNow.Hour;

        // Rough approximation
        if (utcNow.DayOfWeek == DayOfWeek.Saturday || utcNow.DayOfWeek == DayOfWeek.Sunday)
            return "Closed";

        return "Open"; // Simplified for demo
    }
}
