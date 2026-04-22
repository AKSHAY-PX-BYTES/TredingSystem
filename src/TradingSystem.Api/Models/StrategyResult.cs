namespace TradingSystem.Api.Models;

public class StrategyResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty; // BUY, SELL, HOLD
    public decimal Confidence { get; set; } // 0 to 1
    public string Explanation { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public TechnicalIndicators? Indicators { get; set; }
    public PredictionResult? Prediction { get; set; }
    public string? SentimentSummary { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<string> Reasons { get; set; } = new();
}
