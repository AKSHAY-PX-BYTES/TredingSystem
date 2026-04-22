using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface INewsService
{
    Task<NewsAnalysisResponse> AnalyzeAsync(NewsAnalysisRequest request);
    Task<List<NewsItem>> GetLatestNewsAsync(string symbol);
    Task<string> GetOverallSentimentAsync(string symbol);
}

public class NewsService : INewsService
{
    private readonly ILogger<NewsService> _logger;

    // Keywords for sentiment analysis
    private static readonly Dictionary<string, decimal> _positiveWords = new()
    {
        ["surge"] = 0.8m, ["soar"] = 0.9m, ["rally"] = 0.7m, ["gain"] = 0.6m,
        ["profit"] = 0.6m, ["growth"] = 0.7m, ["beat"] = 0.6m, ["exceed"] = 0.7m,
        ["record"] = 0.5m, ["high"] = 0.4m, ["upgrade"] = 0.7m, ["bullish"] = 0.8m,
        ["buy"] = 0.5m, ["strong"] = 0.5m, ["positive"] = 0.6m, ["innovation"] = 0.5m,
        ["breakthrough"] = 0.8m, ["outperform"] = 0.7m, ["revenue"] = 0.3m, ["up"] = 0.4m,
        ["boost"] = 0.6m, ["optimistic"] = 0.7m, ["recovery"] = 0.5m, ["expand"] = 0.5m,
        ["success"] = 0.6m, ["impressive"] = 0.7m, ["dividend"] = 0.4m, ["partnership"] = 0.5m,
    };

    private static readonly Dictionary<string, decimal> _negativeWords = new()
    {
        ["crash"] = -0.9m, ["plunge"] = -0.8m, ["drop"] = -0.6m, ["fall"] = -0.5m,
        ["loss"] = -0.6m, ["decline"] = -0.6m, ["miss"] = -0.5m, ["warning"] = -0.6m,
        ["risk"] = -0.4m, ["low"] = -0.3m, ["downgrade"] = -0.7m, ["bearish"] = -0.8m,
        ["sell"] = -0.5m, ["weak"] = -0.5m, ["negative"] = -0.6m, ["concern"] = -0.4m,
        ["bankruptcy"] = -0.9m, ["underperform"] = -0.7m, ["debt"] = -0.4m, ["down"] = -0.4m,
        ["lawsuit"] = -0.6m, ["fraud"] = -0.9m, ["recession"] = -0.7m, ["layoff"] = -0.6m,
        ["scandal"] = -0.8m, ["investigation"] = -0.5m, ["volatility"] = -0.3m, ["cut"] = -0.4m,
    };

    // Simulated news headlines
    private static readonly Dictionary<string, List<NewsItem>> _sampleNews = new()
    {
        ["AAPL"] = new()
        {
            new() { Headline = "Apple Reports Record Revenue in Q4, Beating Analyst Expectations", Source = "Reuters", PublishedAt = DateTime.UtcNow.AddHours(-2), Symbol = "AAPL" },
            new() { Headline = "iPhone 16 Sales Surge as AI Features Drive Strong Demand", Source = "Bloomberg", PublishedAt = DateTime.UtcNow.AddHours(-5), Symbol = "AAPL" },
            new() { Headline = "Apple Announces New Partnership with Leading AI Startup", Source = "TechCrunch", PublishedAt = DateTime.UtcNow.AddHours(-8), Symbol = "AAPL" },
            new() { Headline = "Concerns Over Apple's China Market Amid Trade Tensions", Source = "CNBC", PublishedAt = DateTime.UtcNow.AddDays(-1), Symbol = "AAPL" },
        },
        ["MSFT"] = new()
        {
            new() { Headline = "Microsoft Azure Revenue Growth Exceeds 30% Year-Over-Year", Source = "Reuters", PublishedAt = DateTime.UtcNow.AddHours(-3), Symbol = "MSFT" },
            new() { Headline = "Microsoft's AI Copilot Drives Strong Enterprise Adoption", Source = "Bloomberg", PublishedAt = DateTime.UtcNow.AddHours(-6), Symbol = "MSFT" },
            new() { Headline = "Analysts Upgrade Microsoft Stock on Bullish Cloud Outlook", Source = "MarketWatch", PublishedAt = DateTime.UtcNow.AddHours(-12), Symbol = "MSFT" },
        },
        ["TSLA"] = new()
        {
            new() { Headline = "Tesla Faces Production Challenges at Berlin Gigafactory", Source = "Reuters", PublishedAt = DateTime.UtcNow.AddHours(-1), Symbol = "TSLA" },
            new() { Headline = "Tesla Cybertruck Demand Surges After Price Cut Announcement", Source = "Bloomberg", PublishedAt = DateTime.UtcNow.AddHours(-4), Symbol = "TSLA" },
            new() { Headline = "Concerns Over Increased EV Competition Weigh on Tesla Stock", Source = "CNBC", PublishedAt = DateTime.UtcNow.AddHours(-7), Symbol = "TSLA" },
            new() { Headline = "Tesla's Autonomous Driving Breakthrough Impresses Analysts", Source = "TechCrunch", PublishedAt = DateTime.UtcNow.AddDays(-1), Symbol = "TSLA" },
        },
        ["NVDA"] = new()
        {
            new() { Headline = "NVIDIA Reports Record Data Center Revenue Driven by AI Demand", Source = "Reuters", PublishedAt = DateTime.UtcNow.AddHours(-2), Symbol = "NVDA" },
            new() { Headline = "NVIDIA's New GPU Architecture Sets Industry Benchmark", Source = "Bloomberg", PublishedAt = DateTime.UtcNow.AddHours(-6), Symbol = "NVDA" },
            new() { Headline = "Analysts Predict NVIDIA Stock Rally to Continue on AI Growth", Source = "MarketWatch", PublishedAt = DateTime.UtcNow.AddHours(-10), Symbol = "NVDA" },
        },
    };

    public NewsService(ILogger<NewsService> logger)
    {
        _logger = logger;
    }

    public Task<NewsAnalysisResponse> AnalyzeAsync(NewsAnalysisRequest request)
    {
        _logger.LogInformation("Analyzing {Count} headlines", request.Headlines.Count);

        var results = request.Headlines.Select(AnalyzeHeadline).ToList();

        var avgScore = results.Any() ? results.Average(r => r.Score) : 0;
        var overallSentiment = avgScore > 0.1m ? "Positive" : avgScore < -0.1m ? "Negative" : "Neutral";

        var response = new NewsAnalysisResponse
        {
            Results = results,
            OverallSentiment = overallSentiment,
            AverageScore = Math.Round(avgScore, 3),
            AnalyzedAt = DateTime.UtcNow
        };

        return Task.FromResult(response);
    }

    public Task<List<NewsItem>> GetLatestNewsAsync(string symbol)
    {
        symbol = symbol.ToUpperInvariant();

        if (_sampleNews.TryGetValue(symbol, out var news))
            return Task.FromResult(news);

        // Generate generic news for unknown symbols
        var generic = new List<NewsItem>
        {
            new() { Headline = $"{symbol} Shows Steady Performance in Market Trading", Source = "Reuters", PublishedAt = DateTime.UtcNow.AddHours(-3), Symbol = symbol },
            new() { Headline = $"Analysts Maintain Hold Rating for {symbol}", Source = "Bloomberg", PublishedAt = DateTime.UtcNow.AddHours(-8), Symbol = symbol },
            new() { Headline = $"{symbol} Reports Quarterly Earnings In Line with Expectations", Source = "CNBC", PublishedAt = DateTime.UtcNow.AddDays(-1), Symbol = symbol },
        };
        return Task.FromResult(generic);
    }

    public async Task<string> GetOverallSentimentAsync(string symbol)
    {
        var news = await GetLatestNewsAsync(symbol);
        var request = new NewsAnalysisRequest { Headlines = news.Select(n => n.Headline).ToList() };
        var analysis = await AnalyzeAsync(request);
        return analysis.OverallSentiment;
    }

    private SentimentResult AnalyzeHeadline(string headline)
    {
        var words = headline.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var score = 0m;
        var matchCount = 0;

        foreach (var word in words)
        {
            var cleanWord = new string(word.Where(char.IsLetter).ToArray());
            if (_positiveWords.TryGetValue(cleanWord, out var posScore))
            {
                score += posScore;
                matchCount++;
            }
            if (_negativeWords.TryGetValue(cleanWord, out var negScore))
            {
                score += negScore;
                matchCount++;
            }
        }

        // Normalize score to -1 to 1
        if (matchCount > 0) score /= matchCount;
        score = Math.Clamp(score, -1m, 1m);

        var sentiment = score > 0.1m ? "Positive" : score < -0.1m ? "Negative" : "Neutral";
        var confidence = Math.Min(1m, Math.Abs(score) + (matchCount > 2 ? 0.3m : 0.1m));

        return new SentimentResult
        {
            Headline = headline,
            Sentiment = sentiment,
            Score = Math.Round(score, 3),
            Confidence = Math.Round(confidence, 3)
        };
    }
}
