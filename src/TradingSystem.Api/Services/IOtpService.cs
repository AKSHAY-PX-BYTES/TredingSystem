using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IOtpService
{
    /// <summary>
    /// Generate and send OTP to email
    /// </summary>
    Task<SendOtpResponse> SendOtpAsync(string email);

    /// <summary>
    /// Verify OTP code for email
    /// </summary>
    Task<VerifyOtpResponse> VerifyOtpAsync(string email, string code);

    /// <summary>
    /// Check if email has been verified
    /// </summary>
    Task<bool> IsEmailVerifiedAsync(string email);

    /// <summary>
    /// Clean up expired OTPs
    /// </summary>
    Task CleanupExpiredOtpsAsync();
}
