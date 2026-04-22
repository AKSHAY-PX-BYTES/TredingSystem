using Microsoft.AspNetCore.SignalR;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Hubs;

public class TradingHub : Hub
{
    private readonly IMarketDataService _marketDataService;
    private readonly IStrategyEngine _strategyEngine;
    private readonly ILogger<TradingHub> _logger;

    public TradingHub(
        IMarketDataService marketDataService,
        IStrategyEngine strategyEngine,
        ILogger<TradingHub> logger)
    {
        _marketDataService = marketDataService;
        _strategyEngine = strategyEngine;
        _logger = logger;
    }

    public async Task SubscribeToSymbol(string symbol)
    {
        _logger.LogInformation("Client {ConnectionId} subscribed to {Symbol}", Context.ConnectionId, symbol);
        await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());

        // Send initial data
        var quote = await _marketDataService.GetQuoteAsync(symbol);
        await Clients.Caller.SendAsync("ReceiveQuote", quote);
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        _logger.LogInformation("Client {ConnectionId} unsubscribed from {Symbol}", Context.ConnectionId, symbol);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());
    }

    public async Task RequestStrategy(string symbol)
    {
        var strategy = await _strategyEngine.EvaluateAsync(symbol);
        await Clients.Caller.SendAsync("ReceiveStrategy", strategy);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
