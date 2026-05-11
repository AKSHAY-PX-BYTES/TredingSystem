using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace TradingSystem.Api.BackgroundServices;

/// <summary>
/// Periodically checks price alerts, generates earnings notifications,
/// dividend reminders, options expiry alerts, and breaking news notifications.
/// </summary>
public class NotificationBroadcaster : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBroadcaster> _logger;

    public NotificationBroadcaster(IServiceScopeFactory scopeFactory, ILogger<NotificationBroadcaster> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPriceAlerts();
                await GenerateMarketNotifications();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification broadcaster");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CheckPriceAlerts()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activeAlerts = await db.PriceAlerts
            .Where(a => a.IsActive && !a.IsTriggered)
            .ToListAsync();

        // In a real implementation, we'd check current prices against alerts
        // For now, randomly trigger some alerts for demonstration
        var random = new Random();
        foreach (var alert in activeAlerts.Take(1)) // Limit to 1 per cycle
        {
            if (random.NextDouble() > 0.95) // 5% chance per cycle
            {
                alert.IsTriggered = true;
                alert.TriggeredAt = DateTime.UtcNow;

                db.Notifications.Add(new NotificationEntity
                {
                    UserId = alert.UserId,
                    Type = "PriceAlert",
                    Title = $"Price Alert: {alert.Symbol}",
                    Message = $"{alert.Symbol} has reached your target price of ${alert.TargetPrice} ({alert.Direction})",
                    Symbol = alert.Symbol
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task GenerateMarketNotifications()
    {
        // Generate periodic market notifications for all users
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var random = new Random();
        if (random.NextDouble() > 0.9) // 10% chance per cycle
        {
            var users = await db.Users.Where(u => u.Plan != "Free" || u.TrialEndsAt > DateTime.UtcNow).ToListAsync();
            var newsTypes = new[]
            {
                ("Earnings", "📊 Earnings Alert", "AAPL Q2 earnings beat expectations. EPS: $1.52 vs $1.43 expected."),
                ("Dividend", "💰 Dividend Payment", "MSFT ex-dividend date tomorrow. Yield: 0.82%"),
                ("OptionsExpiry", "📋 Options Expiry", "Weekly options expire Friday. 5 positions in your watchlist affected."),
                ("BreakingNews", "📰 Breaking News", "Fed announces rate decision. Markets react with +1.2% move.")
            };

            var (type, title, message) = newsTypes[random.Next(newsTypes.Length)];

            foreach (var user in users.Take(5)) // Limit notifications
            {
                db.Notifications.Add(new NotificationEntity
                {
                    UserId = user.Id,
                    Type = type,
                    Title = title,
                    Message = message
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
