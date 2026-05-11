namespace TradingSystem.Api.Data.Entities;

public class SubscriptionEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Plan { get; set; } = "Free"; // Free, Pro, Premium, Enterprise
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime TrialEndsAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsTrialUsed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public decimal PricePerMonth { get; set; } = 0;
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public UserEntity? User { get; set; }
}
