namespace TradingSystem.Api.Data.Entities;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trader";
    public string Plan { get; set; } = "Free";
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime TrialEndsAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsTrialUsed { get; set; } = false;
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredCurrency { get; set; } = "USD";
}
