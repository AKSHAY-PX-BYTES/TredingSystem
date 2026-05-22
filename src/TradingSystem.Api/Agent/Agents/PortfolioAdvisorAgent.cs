using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

public class PortfolioAdvisorAgent : BaseAgent
{
    private readonly AgentOrchestrator _orchestrator;

    public override string Name => "Portfolio Advisor";
    public override string Description => "Suggests rebalancing based on signal performance & diversification";
    public override TimeSpan Interval => TimeSpan.FromHours(1);

    private static readonly Dictionary<string, string> SymbolSectors = new()
    {
        ["AAPL"] = "Technology",
        ["MSFT"] = "Technology",
        ["GOOGL"] = "Technology",
        ["AMZN"] = "Consumer",
        ["TSLA"] = "Automotive",
        ["NVDA"] = "Semiconductors",
        ["META"] = "Technology",
        ["NFLX"] = "Entertainment",
        ["RELIANCE.NS"] = "Conglomerate",
        ["TCS.NS"] = "IT Services"
    };

    public PortfolioAdvisorAgent(AgentOrchestrator orchestrator, ILogger<PortfolioAdvisorAgent> logger)
        : base(logger)
    {
        _orchestrator = orchestrator;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var signals = _orchestrator.GetRecentSignals(50);
        var sentiments = _orchestrator.GetRecentSentiments(50);

        var sectorSignals = signals
            .Where(s => SymbolSectors.ContainsKey(s.Symbol))
            .GroupBy(s => SymbolSectors[s.Symbol])
            .Select(g => new
            {
                Sector = g.Key,
                BuyCount = g.Count(s => s.Signal == SignalType.Buy),
                SellCount = g.Count(s => s.Signal == SignalType.Sell),
                AvgConfidence = g.Average(s => (double)s.Confidence)
            })
            .ToList();

        var recommendations = new List<string>();

        var techSignals = sectorSignals.FirstOrDefault(s => s.Sector == "Technology");
        if (techSignals != null && techSignals.BuyCount > 3)
        {
            recommendations.Add("⚠️ High concentration in Technology sector — consider diversifying into other sectors");
        }

        var bearishSymbols = sentiments
            .Where(s => s.Sentiment == "Bearish")
            .Select(s => s.Symbol)
            .Distinct()
            .ToList();

        if (bearishSymbols.Count >= 3)
        {
            recommendations.Add($"📉 Bearish sentiment detected in {string.Join(", ", bearishSymbols)} — consider reducing exposure");
        }

        var strongBuys = signals.Where(s => s.Signal == SignalType.Buy && s.Confidence >= 80).ToList();
        if (strongBuys.Any())
        {
            recommendations.Add($"🎯 High-confidence opportunities: {string.Join(", ", strongBuys.Select(s => s.Symbol).Distinct())}");
        }

        State.LastMessage = recommendations.Any()
            ? $"{recommendations.Count} recommendations generated"
            : "Portfolio looks balanced — no action needed";

        await Task.CompletedTask;
    }
}
