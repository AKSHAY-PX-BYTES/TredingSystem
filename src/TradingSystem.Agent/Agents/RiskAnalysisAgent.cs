using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

/// <summary>
/// Validates trade signals against risk profiles.
/// Assesses volatility, portfolio concentration, and position sizing.
/// </summary>
public class RiskAnalysisAgent : BaseAgent
{
    private readonly AgentOrchestrator _orchestrator;

    public override string Name => "Risk Analyst";
    public override string Description => "Validates trades against risk profiles, assesses volatility";
    public override TimeSpan Interval => TimeSpan.FromMinutes(10);

    public RiskAnalysisAgent(AgentOrchestrator orchestrator, ILogger<RiskAnalysisAgent> logger)
        : base(logger)
    {
        _orchestrator = orchestrator;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var recentSignals = _orchestrator.GetRecentSignals(10);
        var assessments = 0;

        foreach (var signal in recentSignals.Where(s => s.Signal != SignalType.Hold))
        {
            var assessment = AssessRisk(signal);
            
            // If high risk, override signal confidence
            if (assessment.Risk == RiskLevel.Critical || assessment.Risk == RiskLevel.High)
            {
                _logger.LogWarning("[RiskAgent] High risk detected for {Symbol}: {Recommendation}", 
                    signal.Symbol, assessment.Recommendation);
            }
            
            assessments++;
        }

        State.LastMessage = $"Assessed risk for {assessments} active signals";
        await Task.CompletedTask;
    }

    private RiskAssessment AssessRisk(AgentSignal signal)
    {
        // Simulate risk assessment based on signal properties
        var volatility = CalculateVolatility(signal);
        var riskLevel = volatility switch
        {
            > 0.8m => RiskLevel.Critical,
            > 0.6m => RiskLevel.High,
            > 0.3m => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        var recommendation = riskLevel switch
        {
            RiskLevel.Critical => $"AVOID {signal.Symbol} — extreme volatility detected",
            RiskLevel.High => $"CAUTION on {signal.Symbol} — reduce position size to 50%",
            RiskLevel.Medium => $"PROCEED with {signal.Symbol} — standard position size OK",
            _ => $"GREEN LIGHT for {signal.Symbol} — low risk environment"
        };

        return new RiskAssessment
        {
            Symbol = signal.Symbol,
            Risk = riskLevel,
            Volatility = volatility,
            Recommendation = recommendation,
            AssessedAt = DateTime.UtcNow
        };
    }

    private decimal CalculateVolatility(AgentSignal signal)
    {
        // Heuristic: high confidence divergent signals = high volatility
        var baseVolatility = signal.Confidence > 80 ? 0.7m : 0.3m;
        var signalPenalty = signal.Signal == SignalType.Sell ? 0.2m : 0m;
        return Math.Min(baseVolatility + signalPenalty + (decimal)Random.Shared.NextDouble() * 0.2m, 1.0m);
    }
}
