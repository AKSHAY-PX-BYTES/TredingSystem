using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        // Register agents individually so DI can resolve them
        services.AddSingleton<MarketWatcherAgent>();
        services.AddSingleton<SignalGeneratorAgent>();
        services.AddSingleton<RiskAnalysisAgent>();
        services.AddSingleton<NewsSentimentAgent>();
        services.AddSingleton<AlertAgent>();
        services.AddSingleton<PortfolioAdvisorAgent>();

        // Register IAgent collection manually to avoid resolution issues
        services.AddSingleton<IEnumerable<IAgent>>(sp => new IAgent[]
        {
            sp.GetRequiredService<MarketWatcherAgent>(),
            sp.GetRequiredService<SignalGeneratorAgent>(),
            sp.GetRequiredService<RiskAnalysisAgent>(),
            sp.GetRequiredService<NewsSentimentAgent>(),
            sp.GetRequiredService<AlertAgent>(),
            sp.GetRequiredService<PortfolioAdvisorAgent>()
        });

        // Register orchestrator
        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<IAgentOrchestrator>(sp => sp.GetRequiredService<AgentOrchestrator>());
        services.AddHostedService(sp => sp.GetRequiredService<AgentOrchestrator>());

        return services;
    }
}
