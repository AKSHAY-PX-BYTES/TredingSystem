namespace TradingSystem.Api.Models;

public enum Exchange
{
    NSE,
    BSE,
    NYSE,
    NASDAQ,
    LSE,
    TSE
}

public class MarketIndex
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Exchange Exchange { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Change => Math.Round(CurrentValue - PreviousClose, 2);
    public decimal ChangePercent => PreviousClose != 0 ? Math.Round((CurrentValue - PreviousClose) / PreviousClose * 100, 2) : 0;
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = "Open"; // Open, Closed, Pre-Market
}

public class ExchangeStock
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Exchange Exchange { get; set; }
    public string Sector { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Change => Math.Round(CurrentPrice - PreviousClose, 2);
    public decimal ChangePercent => PreviousClose != 0 ? Math.Round((CurrentPrice - PreviousClose) / PreviousClose * 100, 2) : 0;
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public long Volume { get; set; }
    public decimal MarketCap { get; set; }
    public List<decimal> PriceHistory { get; set; } = new(); // 20 sparkline points
}

public class MarketOverview
{
    public List<MarketIndex> Indices { get; set; } = new();
    public Dictionary<string, List<ExchangeStock>> ExchangeStocks { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class ExchangeData
{
    public string ExchangeName { get; set; } = string.Empty;
    public string ExchangeCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Timezone { get; set; } = string.Empty;
    public List<MarketIndex> Indices { get; set; } = new();
    public List<ExchangeStock> TopGainers { get; set; } = new();
    public List<ExchangeStock> TopLosers { get; set; } = new();
    public List<ExchangeStock> MostActive { get; set; } = new();
    public List<ExchangeStock> AllStocks { get; set; } = new();
    public bool IsLive { get; set; }
    // F&O specific data (only populated for FNO exchange)
    public List<FnoContract> EquityFutures { get; set; } = new();
    public List<FnoContract> EquityOptions { get; set; } = new();
    public List<FnoContract> CommodityFutures { get; set; } = new();
    public List<FnoContract> CommodityOptions { get; set; } = new();
}

public class FnoContract
{
    public string Symbol { get; set; } = string.Empty;
    public string UnderlyingName { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty; // Equity, Commodity
    public string InstrumentType { get; set; } = string.Empty; // FUT, CE, PE
    public string Expiry { get; set; } = string.Empty;
    public decimal StrikePrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public long OpenInterest { get; set; }
    public long OIChange { get; set; }
    public decimal LotSize { get; set; }
    public string Signal { get; set; } = string.Empty; // Bullish, Bearish, Neutral
    public decimal ImpliedVolatility { get; set; }
}
