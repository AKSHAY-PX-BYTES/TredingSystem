using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

public class MarketWatcherAgent : BaseAgent
{
    private readonly HttpClient _httpClient;
    private readonly AgentOrchestrator _orchestrator;
    
    public override string Name => "Market Watcher";
    public override string Description => "Monitors price changes every 5 min, detects significant movements";
    public override TimeSpan Interval => TimeSpan.FromMinutes(5);

    private readonly string[] _watchSymbols = { "AAPL", "MSFT", "GOOGL", "TSLA", "AMZN", "NVDA", "META", "NFLX", "RELIANCE.NS", "TCS.NS" };
    private readonly Dictionary<string, decimal> _lastPrices = new();

    public MarketWatcherAgent(IHttpClientFactory httpClientFactory, AgentOrchestrator orchestrator, ILogger<MarketWatcherAgent> logger) 
        : base(logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentClient");
        _orchestrator = orchestrator;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var symbol in _watchSymbols)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/market/quote/{symbol}", cancellationToken);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (data.TryGetProperty("data", out var quoteData) && 
                    quoteData.TryGetProperty("price", out var priceEl))
                {
                    var currentPrice = priceEl.GetDecimal();
                    
                    if (_lastPrices.TryGetValue(symbol, out var lastPrice) && lastPrice > 0)
                    {
                        var changePercent = ((currentPrice - lastPrice) / lastPrice) * 100;
                        
                        if (Math.Abs(changePercent) >= 2m)
                        {
                            var signal = new AgentSignal
                            {
                                Symbol = symbol,
                                Signal = changePercent > 0 ? SignalType.Buy : SignalType.Sell,
                                Confidence = Math.Min(Math.Abs(changePercent) * 10, 95),
                                Reason = $"Price moved {changePercent:+0.00;-0.00}% in 5 min ({lastPrice:F2} → {currentPrice:F2})",
                                Source = "MarketWatcher",
                                GeneratedAt = DateTime.UtcNow
                            };
                            _orchestrator.AddSignal(signal);
                            _logger.LogInformation("[MarketWatcher] Signal: {Symbol} {Signal} ({Change:+0.00}%)", symbol, signal.Signal, changePercent);
                        }
                    }
                    
                    _lastPrices[symbol] = currentPrice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[MarketWatcher] Failed to fetch {Symbol}: {Error}", symbol, ex.Message);
            }
        }

        State.LastMessage = $"Monitored {_watchSymbols.Length} symbols, tracking {_lastPrices.Count} prices";
    }
}
