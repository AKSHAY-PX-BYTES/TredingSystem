using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(string username, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(string username);
    Task MarkAsReadAsync(string username, long notificationId);
    Task MarkAllAsReadAsync(string username);
    Task CreateNotificationAsync(int userId, string type, string title, string message, string? symbol = null);
    Task<List<PriceAlertDto>> GetPriceAlertsAsync(string username);
    Task<PriceAlertDto?> CreatePriceAlertAsync(string username, CreatePriceAlertRequest request);
    Task<bool> DeletePriceAlertAsync(string username, int alertId);
    Task CheckAndTriggerPriceAlertsAsync();
}

public class NotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(string username, int page = 1, int pageSize = 20)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new();

        return await db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                Symbol = n.Symbol,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return 0;
        return await db.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
    }

    public async Task MarkAsReadAsync(string username, long notificationId)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return;

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == user.Id);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return;

        var unread = await db.Notifications.Where(n => n.UserId == user.Id && !n.IsRead).ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(int userId, string type, string title, string message, string? symbol = null)
    {
        using var db = CreateDbContext();
        db.Notifications.Add(new NotificationEntity
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Symbol = symbol
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<PriceAlertDto>> GetPriceAlertsAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new();

        return await db.PriceAlerts
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PriceAlertDto
            {
                Id = a.Id,
                Symbol = a.Symbol,
                TargetPrice = a.TargetPrice,
                ThresholdPercent = a.ThresholdPercent,
                Direction = a.Direction,
                IsTriggered = a.IsTriggered,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PriceAlertDto?> CreatePriceAlertAsync(string username, CreatePriceAlertRequest request)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return null;

        var alert = new PriceAlertEntity
        {
            UserId = user.Id,
            Symbol = request.Symbol.ToUpper(),
            TargetPrice = request.TargetPrice,
            ThresholdPercent = request.ThresholdPercent,
            Direction = request.Direction
        };
        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync();

        return new PriceAlertDto
        {
            Id = alert.Id,
            Symbol = alert.Symbol,
            TargetPrice = alert.TargetPrice,
            ThresholdPercent = alert.ThresholdPercent,
            Direction = alert.Direction,
            IsActive = true,
            CreatedAt = alert.CreatedAt
        };
    }

    public async Task<bool> DeletePriceAlertAsync(string username, int alertId)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return false;

        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == user.Id);
        if (alert == null) return false;

        db.PriceAlerts.Remove(alert);
        await db.SaveChangesAsync();
        return true;
    }

    public Task CheckAndTriggerPriceAlertsAsync()
    {
        _logger.LogDebug("Price alert check cycle");
        return Task.CompletedTask;
    }
}
