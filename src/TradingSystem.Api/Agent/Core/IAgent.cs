using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Core;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    TimeSpan Interval { get; }
    AgentState State { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IAgentOrchestrator
{
    List<AgentState> GetAllStates();
    List<AgentSignal> GetRecentSignals(int count = 20);
    List<SentimentScore> GetRecentSentiments(int count = 20);
    AgentSystemStats GetStats();
    AgentDashboardResponse GetDashboard();
    Task TriggerAgentAsync(string agentName);
    void EnableAgent(string agentName, bool enabled);
}
