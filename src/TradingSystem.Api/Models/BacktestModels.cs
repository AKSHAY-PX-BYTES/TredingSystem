namespace TradingSystem.Api.Models;

public class BacktestRequest
{
    public string Symbol { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
    public decimal InitialCapital { get; set; } = 10000m;
    public string Strategy { get; set; } = "Combined"; // Combined, TechnicalOnly, SentimentOnly
}

public class BacktestTrade
{
    public int TradeNumber { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public string Signal { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? ProfitLoss { get; set; }
    public decimal? ProfitLossPercent { get; set; }
    public decimal PortfolioValue { get; set; }
}

public class BacktestResult
{
    public string Symbol { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public List<BacktestTrade> Trades { get; set; } = new();
    public List<EquityPoint> EquityCurve { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

public class EquityPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}
