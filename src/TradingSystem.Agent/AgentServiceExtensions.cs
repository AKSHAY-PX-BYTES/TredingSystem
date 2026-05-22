using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Agent.Agents;
using TradingSystem.Agent.Core;

namespace TradingSystem.Agent;

public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers all AI Trading Agents and the orchestrator as hosted services.
    /// </summary>
    public static IServiceCollection AddTradingAgents(this IServiceCollection services, string apiBaseUrl)
    {
        // Register HttpClient for agents to call the existing API
        services.AddHttpClient("AgentClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register orchestrator as singleton (it's the background service)
        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<IAgentOrchestrator>(sp => sp.GetRequiredService<AgentOrchestrator>());

        // Register all agents
        services.AddSingleton<IAgent, MarketWatcherAgent>();
        services.AddSingleton<IAgent, SignalGeneratorAgent>();
        services.AddSingleton<IAgent, RiskAnalysisAgent>();
        services.AddSingleton<IAgent, NewsSentimentAgent>();
        services.AddSingleton<IAgent, AlertAgent>();
        services.AddSingleton<IAgent, PortfolioAdvisorAgent>();

        // Register orchestrator as hosted service
        services.AddHostedService(sp => sp.GetRequiredService<AgentOrchestrator>());

        return services;
    }
}
