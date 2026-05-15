namespace TradingSystem.Api.Data.Entities;

public class PaymentEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "created"; // created, paid, failed, refunded
    public string? PaymentMethod { get; set; }
    public string? Signature { get; set; }
    public bool IsAnnual { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? RawResponse { get; set; }

    public UserEntity? User { get; set; }
}
