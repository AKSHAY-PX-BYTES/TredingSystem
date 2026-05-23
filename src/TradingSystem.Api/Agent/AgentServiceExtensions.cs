using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Agent.Agents;
using TradingSystem.Agent.Core;

namespace TradingSystem.Agent;

public static class AgentServiceExtensions
{
    public static IServiceCollection AddTradingAgents(this IServiceCollection services, string apiBaseUrl)
    {
        services.AddHttpClient("AgentClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Shared signal store (breaks circular dependency)
        services.AddSingleton<AgentSignalStore>();

        // Register orchestrator
        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<IAgentOrchestrator>(sp => sp.GetRequiredService<AgentOrchestrator>());

        services.AddSingleton<IAgent, MarketWatcherAgent>();
        services.AddSingleton<IAgent, SignalGeneratorAgent>();
        services.AddSingleton<IAgent, RiskAnalysisAgent>();
        services.AddSingleton<IAgent, NewsSentimentAgent>();
        services.AddSingleton<IAgent, AlertAgent>();
        services.AddSingleton<IAgent, PortfolioAdvisorAgent>();

        services.AddHostedService(sp => sp.GetRequiredService<AgentOrchestrator>());

        return services;
    }
}
