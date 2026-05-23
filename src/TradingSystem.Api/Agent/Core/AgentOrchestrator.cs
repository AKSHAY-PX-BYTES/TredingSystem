using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Core;

public class AgentOrchestrator : BackgroundService, IAgentOrchestrator
{
    private readonly IEnumerable<IAgent> _agents;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly AgentSignalStore _store;
    private readonly AgentSystemStats _stats = new() { SystemStartedAt = DateTime.UtcNow };

    public AgentOrchestrator(IEnumerable<IAgent> agents, AgentSignalStore store, ILogger<AgentOrchestrator> logger)
    {
        _agents = agents;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent Orchestrator started with {Count} agents", _agents.Count());

        var tasks = _agents.Select(agent => RunAgentLoop(agent, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunAgentLoop(IAgent agent, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(5, 30)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await agent.ExecuteAsync(stoppingToken);
                _stats.TotalRuns++;

                if (agent.State.Status == AgentStatus.Error)
                    _stats.TotalErrors++;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestrator error for agent {Agent}", agent.Name);
            }

            await Task.Delay(agent.Interval, stoppingToken);
        }
    }

    public List<AgentState> GetAllStates() =>
        _agents.Select(a => a.State).ToList();

    public List<AgentSignal> GetRecentSignals(int count = 20) =>
        _store.GetRecentSignals(count);

    public List<SentimentScore> GetRecentSentiments(int count = 20) =>
        _store.GetRecentSentiments(count);

    public AgentSystemStats GetStats()
    {
        _stats.SignalsGenerated = _store.SignalsCount;
        _stats.AlertsSent = _store.AlertCount;
        return _stats;
    }

    public AgentDashboardResponse GetDashboard() => new()
    {
        Agents = GetAllStates(),
        RecentSignals = GetRecentSignals(),
        RecentSentiments = GetRecentSentiments(),
        Stats = GetStats()
    };

    public async Task TriggerAgentAsync(string agentName)
    {
        var agent = _agents.FirstOrDefault(a => a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));
        if (agent != null)
        {
            _logger.LogInformation("Manual trigger for agent: {Agent}", agentName);
            await agent.ExecuteAsync(CancellationToken.None);
        }
    }

    public void EnableAgent(string agentName, bool enabled)
    {
        var agent = _agents.FirstOrDefault(a => a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));
        if (agent != null)
        {
            agent.State.IsEnabled = enabled;
            agent.State.Status = enabled ? AgentStatus.Idle : AgentStatus.Disabled;
            _logger.LogInformation("Agent {Agent} {Action}", agentName, enabled ? "enabled" : "disabled");
        }
    }
}
