using System.Collections.Concurrent;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Core;

/// <summary>
/// Shared signal/sentiment store that agents write to and orchestrator reads from.
/// Breaks the circular dependency between agents and orchestrator.
/// </summary>
public class AgentSignalStore
{
    private readonly ConcurrentBag<AgentSignal> _signals = new();
    private readonly ConcurrentBag<SentimentScore> _sentiments = new();
    private int _alertCount;

    public void AddSignal(AgentSignal signal) => _signals.Add(signal);
    public void AddSentiment(SentimentScore sentiment) => _sentiments.Add(sentiment);
    public void IncrementAlerts() => Interlocked.Increment(ref _alertCount);
    public int AlertCount => _alertCount;

    public List<AgentSignal> GetRecentSignals(int count = 20) =>
        _signals.OrderByDescending(s => s.GeneratedAt).Take(count).ToList();

    public List<SentimentScore> GetRecentSentiments(int count = 20) =>
        _sentiments.OrderByDescending(s => s.AnalyzedAt).Take(count).ToList();

    public int SignalsCount => _signals.Count;
}
