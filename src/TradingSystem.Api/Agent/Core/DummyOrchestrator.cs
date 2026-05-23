using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Core;

/// <summary>
/// No-op orchestrator used when agents are disabled (e.g., free-tier hosting with limited RAM).
/// </summary>
public class DummyOrchestrator : IAgentOrchestrator
{
    public List<AgentState> GetAllStates() => new()
    {
        new AgentState { AgentName = "Market Watcher", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled },
        new AgentState { AgentName = "Signal Generator", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled },
        new AgentState { AgentName = "Risk Analyst", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled },
        new AgentState { AgentName = "News Sentiment", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled },
        new AgentState { AgentName = "Alert Agent", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled },
        new AgentState { AgentName = "Portfolio Advisor", Description = "Agents disabled in current environment", Status = AgentStatus.Disabled }
    };

    public List<AgentSignal> GetRecentSignals(int count = 20) => new();
    public List<SentimentScore> GetRecentSentiments(int count = 20) => new();
    public AgentSystemStats GetStats() => new() { SystemStartedAt = DateTime.UtcNow };
    public AgentDashboardResponse GetDashboard() => new()
    {
        Agents = GetAllStates(),
        RecentSignals = new(),
        RecentSentiments = new(),
        Stats = GetStats()
    };
    public Task TriggerAgentAsync(string agentName) => Task.CompletedTask;
    public void EnableAgent(string agentName, bool enabled) { }
}
