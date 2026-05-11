namespace TradingSystem.Api.Data.Entities;

public class NotificationEntity
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty; // PriceAlert, Earnings, Dividend, OptionsExpiry, BreakingNews
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? Data { get; set; } // JSON payload
    public bool IsRead { get; set; } = false;
    public bool IsEmailed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public UserEntity? User { get; set; }
}

public class PriceAlertEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal ThresholdPercent { get; set; } = 5.0m;
    public string Direction { get; set; } = "Above"; // Above, Below, Both
    public bool IsTriggered { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TriggeredAt { get; set; }

    public UserEntity? User { get; set; }
}
