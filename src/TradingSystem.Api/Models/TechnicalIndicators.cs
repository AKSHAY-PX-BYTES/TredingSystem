namespace TradingSystem.Api.Models;

public class TechnicalIndicators
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public decimal CurrentPrice { get; set; }

    // Simple Moving Averages
    public decimal SMA20 { get; set; }
    public decimal SMA50 { get; set; }
    public decimal SMA200 { get; set; }

    // Exponential Moving Averages
    public decimal EMA12 { get; set; }
    public decimal EMA26 { get; set; }

    // RSI
    public decimal RSI { get; set; }

    // MACD
    public decimal MACD => EMA12 - EMA26;

    // Bollinger Bands
    public decimal BollingerUpper { get; set; }
    public decimal BollingerMiddle { get; set; }
    public decimal BollingerLower { get; set; }

    // Trend
    public string Trend => GetTrend();

    private string GetTrend()
    {
        if (CurrentPrice > SMA20 && SMA20 > SMA50)
            return "Bullish";
        if (CurrentPrice < SMA20 && SMA20 < SMA50)
            return "Bearish";
        return "Neutral";
    }
}
