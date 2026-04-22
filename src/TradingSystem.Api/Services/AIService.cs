using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IAIService
{
    Task<PredictionResult> PredictAsync(string symbol);
}

public class AIService : IAIService
{
    private readonly IMarketDataService _marketDataService;
    private readonly INewsService _newsService;
    private readonly ILogger<AIService> _logger;

    public AIService(
        IMarketDataService marketDataService,
        INewsService newsService,
        ILogger<AIService> logger)
    {
        _marketDataService = marketDataService;
        _newsService = newsService;
        _logger = logger;
    }

    public async Task<PredictionResult> PredictAsync(string symbol)
    {
        _logger.LogInformation("Running AI prediction for {Symbol}", symbol);

        // Get market data and indicators
        var indicators = await _marketDataService.GetIndicatorsAsync(symbol);
        var quote = await _marketDataService.GetQuoteAsync(symbol);

        // Get sentiment
        var sentiment = await _newsService.GetOverallSentimentAsync(symbol);

        // Feature engineering
        var features = CalculateFeatures(indicators, sentiment);

        // ML-like scoring (combines multiple signals)
        var predictionScore = CalculatePredictionScore(features);

        // Calculate predicted price change
        var priceChangePercent = predictionScore * 0.05m; // max 5% move
        var predictedPrice = Math.Round(quote.CurrentPrice * (1 + priceChangePercent), 2);

        var direction = predictionScore > 0.1m ? "Up" : predictionScore < -0.1m ? "Down" : "Flat";
        var confidence = Math.Round(Math.Min(1m, Math.Abs(predictionScore) + 0.3m), 3);

        return new PredictionResult
        {
            Symbol = symbol.ToUpperInvariant(),
            CurrentPrice = quote.CurrentPrice,
            PredictedPrice = predictedPrice,
            PredictedChange = Math.Round(predictedPrice - quote.CurrentPrice, 2),
            PredictedChangePercent = Math.Round(priceChangePercent * 100, 2),
            Direction = direction,
            Confidence = confidence,
            TimeHorizon = "1 Day",
            PredictedAt = DateTime.UtcNow,
            FeatureImportance = features
        };
    }

    private Dictionary<string, decimal> CalculateFeatures(TechnicalIndicators indicators, string sentiment)
    {
        var features = new Dictionary<string, decimal>();

        // RSI Signal (-1 to 1): oversold is bullish, overbought is bearish
        var rsiSignal = indicators.RSI < 30 ? 0.8m
            : indicators.RSI > 70 ? -0.8m
            : (50m - indicators.RSI) / 50m * -1;
        features["RSI_Signal"] = Math.Round(rsiSignal, 3);

        // SMA Crossover Signal
        var smaCross = indicators.CurrentPrice > indicators.SMA20 ? 0.5m : -0.5m;
        if (indicators.SMA20 > indicators.SMA50) smaCross += 0.3m;
        else smaCross -= 0.3m;
        features["SMA_Signal"] = Math.Round(Math.Clamp(smaCross, -1m, 1m), 3);

        // MACD Signal
        var macdSignal = indicators.MACD > 0 ? 0.6m : -0.6m;
        features["MACD_Signal"] = Math.Round(macdSignal, 3);

        // Bollinger Band Signal
        var bbPosition = 0m;
        if (indicators.BollingerUpper != indicators.BollingerLower)
        {
            bbPosition = (indicators.CurrentPrice - indicators.BollingerLower) /
                         (indicators.BollingerUpper - indicators.BollingerLower);
            bbPosition = (0.5m - bbPosition) * 2; // oversold is positive
        }
        features["BB_Signal"] = Math.Round(Math.Clamp(bbPosition, -1m, 1m), 3);

        // Sentiment Signal
        var sentimentScore = sentiment switch
        {
            "Positive" => 0.7m,
            "Negative" => -0.7m,
            _ => 0m
        };
        features["Sentiment_Signal"] = sentimentScore;

        // Trend Signal
        var trendSignal = indicators.Trend switch
        {
            "Bullish" => 0.6m,
            "Bearish" => -0.6m,
            _ => 0m
        };
        features["Trend_Signal"] = trendSignal;

        return features;
    }

    private decimal CalculatePredictionScore(Dictionary<string, decimal> features)
    {
        // Weighted combination of signals (simulates ML model output)
        var weights = new Dictionary<string, decimal>
        {
            ["RSI_Signal"] = 0.20m,
            ["SMA_Signal"] = 0.20m,
            ["MACD_Signal"] = 0.15m,
            ["BB_Signal"] = 0.15m,
            ["Sentiment_Signal"] = 0.15m,
            ["Trend_Signal"] = 0.15m,
        };

        var score = 0m;
        foreach (var (feature, value) in features)
        {
            if (weights.TryGetValue(feature, out var weight))
                score += value * weight;
        }

        return Math.Clamp(score, -1m, 1m);
    }
}
