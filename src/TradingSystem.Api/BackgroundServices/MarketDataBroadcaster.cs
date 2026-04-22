using Microsoft.AspNetCore.SignalR;
using TradingSystem.Api.Services;
using TradingSystem.Api.Hubs;

namespace TradingSystem.Api.BackgroundServices;

public class MarketDataBroadcaster : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly ILogger<MarketDataBroadcaster> _logger;
    private static readonly string[] _symbols = { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "NVDA" };

    public MarketDataBroadcaster(
        IServiceProvider serviceProvider,
        IHubContext<TradingHub> hubContext,
        ILogger<MarketDataBroadcaster> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market data broadcaster started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

                foreach (var symbol in _symbols)
                {
                    var quote = await marketDataService.GetQuoteAsync(symbol);
                    await _hubContext.Clients.Group(symbol).SendAsync("ReceiveQuote", quote, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting market data");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Update every 30 seconds
        }
    }
}
