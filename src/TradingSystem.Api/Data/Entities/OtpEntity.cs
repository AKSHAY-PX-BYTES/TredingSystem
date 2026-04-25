namespace TradingSystem.Api.Data.Entities;

/// <summary>
/// Stores one-time passwords for email verification during user registration
/// </summary>
public class OtpEntity
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Code { get; set; } // 6-digit code
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // Typically 5-10 minutes from creation
    public bool IsVerified { get; set; } = false;
}
