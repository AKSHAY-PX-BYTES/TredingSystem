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
            var smtpServer = emailConfig["SmtpServer"] ?? "smtp.mailtrap.io";
            var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];
            var senderEmail = emailConfig["SenderEmail"] ?? "noreply@tredingsystem.com";
            var senderName = emailConfig["SenderName"] ?? "TredingSystem";

            // For development/testing without real email credentials
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning(
                    "⚠️ Email credentials not configured.\n" +
                    "📧 OTP Code for {Email}: {Code}\n" +
                    "💾 To receive emails, set these environment variables on Render:\n" +
                    "   Email__Username (e.g., your-mailtrap-username)\n" +
                    "   Email__Password (e.g., your-mailtrap-password)\n" +
                    "📚 Get Mailtrap credentials from: https://mailtrap.io/api-tokens", 
                    toEmail, code);
                return;
            }

            _logger.LogInformation("📧 Sending OTP email to {Email} via {SmtpServer}:{SmtpPort}", toEmail, smtpServer, smtpPort);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(username, password);
                client.Timeout = 10000; // 10 second timeout

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "🔐 Your OTP Verification Code - TredingSystem",
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
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 0;
            padding: 0;
            background-color: #f5f5f5;
        }}
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 20px; 
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        .header {{ 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; 
            padding: 30px 20px; 
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{ 
            padding: 30px 20px; 
            color: #333;
        }}
        .otp-box {{
            background-color: #f9f9f9;
            border: 2px solid #667eea;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            margin: 25px 0;
        }}
        .otp {{ 
            font-size: 42px; 
            font-weight: bold; 
            color: #667eea;
            letter-spacing: 8px; 
            font-family: 'Courier New', monospace;
        }}
        .expiry {{
            color: #e74c3c;
            font-weight: 600;
            margin-top: 15px;
            font-size: 14px;
        }}
        .footer {{ 
            text-align: center; 
            padding: 20px; 
            color: #999; 
            font-size: 12px;
            border-top: 1px solid #eee;
        }}
        .note {{
            background-color: #f0f8ff;
            border-left: 4px solid #667eea;
            padding: 12px 15px;
            margin: 15px 0;
            font-size: 13px;
            color: #555;
        }}
        a {{
            color: #667eea;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 TredingSystem</h1>
            <p style='margin: 10px 0 0 0; font-size: 14px; opacity: 0.9;'>Email Verification</p>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>Thank you for signing up to TredingSystem! To verify your email address and complete your registration, please use the one-time password (OTP) below:</p>
            
            <div class='otp-box'>
                <div class='otp'>{code}</div>
                <div class='expiry'>⏰ Expires in 10 minutes</div>
            </div>

            <p>This code is valid for a single use only and will expire in 10 minutes.</p>

            <div class='note'>
                <strong>🔒 Security Note:</strong> Never share this code with anyone. TredingSystem support will never ask for your OTP code.
            </div>

            <p>If you didn't sign up for TredingSystem, please ignore this email or <a href='#'>contact us</a>.</p>

            <p>Best regards,<br/>The TredingSystem Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 TredingSystem. All rights reserved.</p>
            <p><a href='#'>Privacy Policy</a> | <a href='#'>Terms of Service</a></p>
        </div>
    </div>
</body>
</html>
";
    }
}
