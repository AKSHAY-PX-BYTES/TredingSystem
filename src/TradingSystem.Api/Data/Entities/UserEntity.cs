namespace TradingSystem.Api.Data.Entities;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trader";
    public string Plan { get; set; } = "Basic";
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
