using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IStrategyEngine
{
    Task<StrategyResult> EvaluateAsync(string symbol);
}

public class StrategyEngine : IStrategyEngine
{
    private readonly IMarketDataService _marketDataService;
    private readonly IAIService _aiService;
    private readonly INewsService _newsService;
    private readonly ILogger<StrategyEngine> _logger;

    public StrategyEngine(
        IMarketDataService marketDataService,
        IAIService aiService,
        INewsService newsService,
        ILogger<StrategyEngine> logger)
    {
        _marketDataService = marketDataService;
        _aiService = aiService;
        _newsService = newsService;
        _logger = logger;
    }

    public async Task<StrategyResult> EvaluateAsync(string symbol)
    {
        _logger.LogInformation("Evaluating strategy for {Symbol}", symbol);

        var indicators = await _marketDataService.GetIndicatorsAsync(symbol);
        var prediction = await _aiService.PredictAsync(symbol);
        var sentiment = await _newsService.GetOverallSentimentAsync(symbol);

        var reasons = new List<string>();
        var signalScore = 0m;

        // Technical Analysis Signals
        // RSI
        if (indicators.RSI < 30)
        {
            signalScore += 0.3m;
            reasons.Add($"RSI is oversold at {indicators.RSI:F1} (below 30), indicating potential upward reversal");
        }
        else if (indicators.RSI > 70)
        {
            signalScore -= 0.3m;
            reasons.Add($"RSI is overbought at {indicators.RSI:F1} (above 70), indicating potential downward reversal");
        }
        else
        {
            reasons.Add($"RSI is neutral at {indicators.RSI:F1}");
        }

        // SMA Trend
        if (indicators.CurrentPrice > indicators.SMA20 && indicators.SMA20 > indicators.SMA50)
        {
            signalScore += 0.25m;
            reasons.Add("Price is above SMA20 and SMA20 > SMA50 — bullish trend alignment");
        }
        else if (indicators.CurrentPrice < indicators.SMA20 && indicators.SMA20 < indicators.SMA50)
        {
            signalScore -= 0.25m;
            reasons.Add("Price is below SMA20 and SMA20 < SMA50 — bearish trend alignment");
        }
        else
        {
            reasons.Add("Moving averages show mixed signals — trend is unclear");
        }

        // MACD
        if (indicators.MACD > 0)
        {
            signalScore += 0.15m;
            reasons.Add($"MACD is positive ({indicators.MACD:F2}), indicating bullish momentum");
        }
        else
        {
            signalScore -= 0.15m;
            reasons.Add($"MACD is negative ({indicators.MACD:F2}), indicating bearish momentum");
        }

        // Bollinger Bands
        if (indicators.CurrentPrice <= indicators.BollingerLower)
        {
            signalScore += 0.15m;
            reasons.Add("Price is at or below lower Bollinger Band — potential bounce");
        }
        else if (indicators.CurrentPrice >= indicators.BollingerUpper)
        {
            signalScore -= 0.15m;
            reasons.Add("Price is at or above upper Bollinger Band — potential pullback");
        }

        // AI Prediction
        if (prediction.Direction == "Up")
        {
            signalScore += 0.2m * prediction.Confidence;
            reasons.Add($"AI model predicts upward movement with {prediction.Confidence:P0} confidence");
        }
        else if (prediction.Direction == "Down")
        {
            signalScore -= 0.2m * prediction.Confidence;
            reasons.Add($"AI model predicts downward movement with {prediction.Confidence:P0} confidence");
        }

        // Sentiment
        if (sentiment == "Positive")
        {
            signalScore += 0.15m;
            reasons.Add("News sentiment is positive — supportive of bullish outlook");
        }
        else if (sentiment == "Negative")
        {
            signalScore -= 0.15m;
            reasons.Add("News sentiment is negative — cautionary for bullish positions");
        }
        else
        {
            reasons.Add("News sentiment is neutral — no strong directional bias from news");
        }

        // Determine Signal
        var signal = signalScore > 0.15m ? "BUY" : signalScore < -0.15m ? "SELL" : "HOLD";
        var confidence = Math.Round(Math.Min(1m, Math.Abs(signalScore) + 0.2m), 3);

        // Generate explanation
        var explanation = signal switch
        {
            "BUY" => $"Multiple indicators suggest a buying opportunity for {symbol}. " +
                      $"The combined technical, AI prediction, and sentiment analysis score is {signalScore:F3} " +
                      $"with {confidence:P0} confidence.",
            "SELL" => $"Multiple indicators suggest selling or shorting {symbol}. " +
                       $"The combined score is {signalScore:F3} with {confidence:P0} confidence.",
            _ => $"Signals are mixed for {symbol}. The combined score is {signalScore:F3}. " +
                 $"Recommend holding current position until clearer signals emerge."
        };

        // Calculate target and stop loss
        var targetMultiplier = signal == "BUY" ? 1.05m : signal == "SELL" ? 0.95m : 1.0m;
        var stopLossMultiplier = signal == "BUY" ? 0.97m : signal == "SELL" ? 1.03m : 1.0m;

        return new StrategyResult
        {
            Symbol = symbol.ToUpperInvariant(),
            Signal = signal,
            Confidence = confidence,
            Explanation = explanation,
            CurrentPrice = indicators.CurrentPrice,
            TargetPrice = Math.Round(indicators.CurrentPrice * targetMultiplier, 2),
            StopLoss = Math.Round(indicators.CurrentPrice * stopLossMultiplier, 2),
            Indicators = indicators,
            Prediction = prediction,
            SentimentSummary = sentiment,
            Reasons = reasons,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
