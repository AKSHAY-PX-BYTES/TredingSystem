using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

public class NewsSentimentAgent : BaseAgent
{
    private readonly HttpClient _httpClient;
    private readonly AgentSignalStore _store;

    public override string Name => "News Sentiment";
    public override string Description => "Reads financial news, scores sentiment for tracked symbols";
    public override TimeSpan Interval => TimeSpan.FromMinutes(30);

    private readonly string[] _symbols = { "AAPL", "MSFT", "GOOGL", "TSLA", "NVDA" };

    private static readonly string[] BullishKeywords = { "surge", "rally", "breakout", "upgrade", "beat", "growth", "record", "bullish", "soar", "profit" };
    private static readonly string[] BearishKeywords = { "crash", "plunge", "downgrade", "miss", "decline", "loss", "bearish", "drop", "sell-off", "warning" };

    public NewsSentimentAgent(IHttpClientFactory httpClientFactory, AgentSignalStore store, ILogger<NewsSentimentAgent> logger)
        : base(logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentClient");
        _store = store;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var analyzed = 0;

        foreach (var symbol in _symbols)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/news?symbol={symbol}&count=5", cancellationToken);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("data", out var newsArray) && newsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var newsItem in newsArray.EnumerateArray())
                    {
                        var headline = newsItem.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(headline)) continue;

                        var sentiment = AnalyzeSentiment(headline);
                        var score = new SentimentScore
                        {
                            Symbol = symbol,
                            Headline = headline.Length > 100 ? headline[..100] + "..." : headline,
                            Score = sentiment.score,
                            Sentiment = sentiment.label,
                            AnalyzedAt = DateTime.UtcNow
                        };
                        _store.AddSentiment(score);
                        analyzed++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[NewsSentiment] Failed for {Symbol}: {Error}", symbol, ex.Message);
            }
        }

        State.LastMessage = $"Analyzed sentiment for {analyzed} news articles across {_symbols.Length} symbols";
    }

    private (decimal score, string label) AnalyzeSentiment(string text)
    {
        var lower = text.ToLower();
        var bullish = BullishKeywords.Count(k => lower.Contains(k));
        var bearish = BearishKeywords.Count(k => lower.Contains(k));

        var total = bullish + bearish;
        if (total == 0) return (0m, "Neutral");

        var score = (decimal)(bullish - bearish) / total;
        var label = score > 0.2m ? "Bullish" : score < -0.2m ? "Bearish" : "Neutral";
        return (score, label);
    }
}
