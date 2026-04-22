namespace TradingSystem.Api.Models;

public class NewsItem
{
    public string Headline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? Symbol { get; set; }
}

public class NewsAnalysisRequest
{
    public List<string> Headlines { get; set; } = new();
    public string? Symbol { get; set; }
}

public class SentimentResult
{
    public string Headline { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty; // Positive, Negative, Neutral
    public decimal Score { get; set; } // -1 to 1
    public decimal Confidence { get; set; } // 0 to 1
}

public class NewsAnalysisResponse
{
    public List<SentimentResult> Results { get; set; } = new();
    public string OverallSentiment { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
