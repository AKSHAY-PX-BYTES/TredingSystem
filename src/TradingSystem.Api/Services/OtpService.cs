using System.Net;
using System.Net.Mail;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services.EmailProviders;
using Microsoft.EntityFrameworkCore;

namespace TradingSystem.Api.Services;

public class OtpService : IOtpService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;
    private readonly IEmailService _emailService;
    private readonly Random _random = new Random();

    public OtpService(AppDbContext context, IConfiguration configuration, ILogger<OtpService> logger, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<SendOtpResponse> SendOtpAsync(string email)
    {
        try
        {
            email = email.Trim().ToLowerInvariant();

            // Check if user already exists with this email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (existingUser != null)
            {
                return new SendOtpResponse
                {
                    Success = false,
                    Error = "Email is already registered"
                };
            }

            // Generate 6-digit code
            string code = _random.Next(100000, 999999).ToString();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(_configuration.GetValue<int>("Otp:ExpiryMinutes", 10));

            // Delete any existing unverified OTPs for this email
            var existingOtps = await _context.Otps
                .Where(o => o.Email == email && !o.IsVerified)
                .ToListAsync();

            _context.Otps.RemoveRange(existingOtps);

            // Create new OTP
            var otp = new OtpEntity
            {
                Email = email,
                Code = code,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsVerified = false
            };

            _context.Otps.Add(otp);
            await _context.SaveChangesAsync();

            // Send email using EmailService (supports multiple providers: Brevo, Mailgun, SendGrid, Resend)
            try
            {
                await _emailService.SendOtpEmailAsync(email, code);
                _logger.LogInformation("📧 OTP email sent successfully to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to send OTP email to {Email}", email);
                return new SendOtpResponse
                {
                    Success = false,
                    Error = "Failed to send OTP email. Please check email provider configuration or try again later."
                };
            }

            return new SendOtpResponse
            {
                Success = true,
                Message = "OTP sent to your email",
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to email: {Email}", email);
            return new SendOtpResponse
            {
                Success = false,
                Error = "Failed to send OTP. Please try again later."
            };
        }
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(string email, string code)
    {
        try
        {
            _logger.LogInformation("Verifying OTP for email: {Email}, code: {Code}", email, code);

            email = email.Trim().ToLowerInvariant();
            code = code.Trim();

            var otp = await _context.Otps
                .Where(o => o.Email == email && o.Code == code && !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                // Check if there's any OTP for this email at all
                var anyOtp = await _context.Otps.AnyAsync(o => o.Email == email);
                _logger.LogWarning("OTP not found for {Email}. Any OTPs exist: {Any}", email, anyOtp);

                return new VerifyOtpResponse
                {
                    Success = false,
                    Error = "Invalid OTP code"
                };
            }

            _logger.LogInformation("Found OTP id={Id}, expires={Expires}, now={Now}", otp.Id, otp.ExpiresAt, DateTime.UtcNow);

            // Check if OTP is expired
            if (DateTime.UtcNow > otp.ExpiresAt)
            {
                return new VerifyOtpResponse
                {
                    Success = false,
                    Error = "OTP has expired"
                };
            }

            // Mark as verified
            otp.IsVerified = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP verified successfully for email: {Email}", email);

            return new VerifyOtpResponse
            {
                Success = true,
                Message = "Email verified successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for email: {Email}. Exception: {Message}", email, ex.ToString());
            return new VerifyOtpResponse
            {
                Success = false,
                Error = $"Failed to verify OTP: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsEmailVerifiedAsync(string email)
    {
        var verifiedOtp = await _context.Otps
            .FirstOrDefaultAsync(o => o.Email == email && o.IsVerified);

        return verifiedOtp != null;
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        try
        {
            var expiredOtps = await _context.Otps
                .Where(o => o.ExpiresAt < DateTime.UtcNow && !o.IsVerified)
                .ToListAsync();

            if (expiredOtps.Count > 0)
            {
                _context.Otps.RemoveRange(expiredOtps);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired OTPs", expiredOtps.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired OTPs");
        }
    }
}
