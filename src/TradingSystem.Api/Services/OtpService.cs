using System.Net;
using System.Net.Mail;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace TradingSystem.Api.Services;

public class OtpService : IOtpService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;
    private readonly Random _random = new Random();

    public OtpService(AppDbContext context, IConfiguration configuration, ILogger<OtpService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SendOtpResponse> SendOtpAsync(string email)
    {
        try
        {
            // Check if user already exists with this email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

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

            // Send email
            await SendEmailAsync(email, code);

            _logger.LogInformation("OTP sent to email: {Email}", email);

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
            var otp = await _context.Otps
                .FirstOrDefaultAsync(o => o.Email == email && o.Code == code && !o.IsVerified);

            if (otp == null)
            {
                return new VerifyOtpResponse
                {
                    Success = false,
                    Error = "Invalid OTP code"
                };
            }

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
            _context.Otps.Update(otp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP verified for email: {Email}", email);

            return new VerifyOtpResponse
            {
                Success = true,
                Message = "Email verified successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for email: {Email}", email);
            return new VerifyOtpResponse
            {
                Success = false,
                Error = "Failed to verify OTP"
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

    private async Task SendEmailAsync(string toEmail, string code)
    {
        try
        {
            var emailConfig = _configuration.GetSection("Email");
            var smtpServer = emailConfig["SmtpServer"];
            var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];
            var senderEmail = emailConfig["SenderEmail"];
            var senderName = emailConfig["SenderName"];

            // For development/testing without real email credentials, just log
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("⚠️ Email credentials not configured in environment. OTP code for {Email}: {Code}. To receive real emails, configure Email__Username and Email__Password on Render.", toEmail, code);
                return;
            }

            _logger.LogInformation("📧 Sending OTP email to {Email} via {SmtpServer}", toEmail, smtpServer);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(username, password);
                client.Timeout = 10000; // 10 second timeout

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Your OTP Verification Code",
                    Body = GenerateEmailBody(code),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Email sent successfully to: {Email}", toEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email to: {Email}", toEmail);
            throw;
        }
    }

    private string GenerateEmailBody(string code)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .otp {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; letter-spacing: 5px; }}
        .footer {{ text-align: center; padding: 10px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>TredingSystem</h1>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>Your one-time password (OTP) for email verification is:</p>
            <div class='otp'>{code}</div>
            <p>This code will expire in 10 minutes.</p>
            <p>If you didn't request this code, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 TredingSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
    }
}
