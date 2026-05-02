namespace TradingSystem.Api.Models;

public class StockData
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal Change => Close - Open;
    public decimal ChangePercent => Open != 0 ? Math.Round((Close - Open) / Open * 100, 2) : 0;
}

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public long Volume { get; set; }
    public decimal Change => CurrentPrice - PreviousClose;
    public decimal ChangePercent => PreviousClose != 0 ? Math.Round((CurrentPrice - PreviousClose) / PreviousClose * 100, 2) : 0;
    public DateTime LastUpdated { get; set; }
    /// <summary>
    /// The native currency of the price data returned by the exchange (e.g. "USD", "INR").
    /// This tells the frontend whether conversion is needed.
    /// </summary>
    public string PriceCurrency { get; set; } = "USD";
    public List<StockData> HistoricalData { get; set; } = new();
}
