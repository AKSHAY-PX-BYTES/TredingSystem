namespace TradingSystem.Api.Models;

public class PredictionResult
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal PredictedChange { get; set; }
    public decimal PredictedChangePercent { get; set; }
    public string Direction { get; set; } = string.Empty; // Up, Down, Flat
    public decimal Confidence { get; set; }
    public string TimeHorizon { get; set; } = "1 Day";
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, decimal> FeatureImportance { get; set; } = new();
}
