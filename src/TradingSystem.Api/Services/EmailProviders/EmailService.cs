using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TradingSystem.Api.Services.EmailProviders;

/// <summary>
/// Interface for email service implementations
/// </summary>
public interface IEmailProvider
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}

/// <summary>
/// Brevo (Formerly Sendinblue) - Free: 300 emails/day
/// Best for: Quick setup, no credit card required, generous free tier
/// </summary>
public class BrevoEmailProvider : IEmailProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailProvider> _logger;
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

    public BrevoEmailProvider(IConfiguration configuration, ILogger<BrevoEmailProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["EmailProviders:Brevo:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning(
                    "⚠️ Brevo API key not configured.\n" +
                    "📧 To enable email delivery:\n" +
                    "   1. Go to https://www.brevo.com/free-email\n" +
                    "   2. Sign up (no credit card needed)\n" +
                    "   3. Go to Settings → SMTP & API → API Tokens\n" +
                    "   4. Generate API key\n" +
                    "   5. Set EmailProviders__Brevo__ApiKey on Render\n" +
                    "   Free Tier: 300 emails/day ✅");
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.Timeout = TimeSpan.FromSeconds(10);

                var payload = new
                {
                    sender = new { name = "TredingSystem", email = "noreply@tredingsystem.com" },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = htmlBody
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via Brevo to: {Email}", toEmail);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Brevo API error: {Status} - {Error}", response.StatusCode, errorContent);
                    throw new Exception($"Brevo API error: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email via Brevo to: {Email}", toEmail);
            throw;
        }
    }
}

/// <summary>
/// Mailgun - Free: 5000 emails/month on free tier
/// Best for: Reliable, sandbox domain included
/// </summary>
public class MailgunEmailProvider : IEmailProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailgunEmailProvider> _logger;
    private const string ApiUrl = "https://api.mailgun.net/v3";

    public MailgunEmailProvider(IConfiguration configuration, ILogger<MailgunEmailProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["EmailProviders:Mailgun:ApiKey"];
            var domain = _configuration["EmailProviders:Mailgun:Domain"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(domain))
            {
                _logger.LogWarning(
                    "⚠️ Mailgun credentials not configured.\n" +
                    "📧 To enable email delivery:\n" +
                    "   1. Go to https://mailgun.com/pricing\n" +
                    "   2. Sign up (free tier: 5000 emails/month)\n" +
                    "   3. Go to API & Domain Management\n" +
                    "   4. Copy API Key and Domain\n" +
                    "   5. Set EmailProviders__Mailgun__ApiKey and Domain on Render");
                return;
            }

            using (var client = new HttpClient())
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
                client.Timeout = TimeSpan.FromSeconds(10);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("from", "TredingSystem <noreply@tredingsystem.com>"),
                    new KeyValuePair<string, string>("to", toEmail),
                    new KeyValuePair<string, string>("subject", subject),
                    new KeyValuePair<string, string>("html", htmlBody)
                });

                var response = await client.PostAsync($"{ApiUrl}/{domain}/messages", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via Mailgun to: {Email}", toEmail);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Mailgun API error: {Status} - {Error}", response.StatusCode, errorContent);
                    throw new Exception($"Mailgun API error: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email via Mailgun to: {Email}", toEmail);
            throw;
        }
    }
}

/// <summary>
/// SendGrid - Free: 100 emails/day on free tier
/// Best for: Reliable, good templates
/// </summary>
public class SendGridEmailProvider : IEmailProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailProvider> _logger;
    private const string ApiUrl = "https://api.sendgrid.com/v3/mail/send";

    public SendGridEmailProvider(IConfiguration configuration, ILogger<SendGridEmailProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["EmailProviders:SendGrid:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning(
                    "⚠️ SendGrid API key not configured.\n" +
                    "📧 To enable email delivery:\n" +
                    "   1. Go to https://sendgrid.com/pricing\n" +
                    "   2. Sign up (free tier: 100 emails/day)\n" +
                    "   3. Go to Settings → API Keys\n" +
                    "   4. Create Full Access API key\n" +
                    "   5. Set EmailProviders__SendGrid__ApiKey on Render");
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.Timeout = TimeSpan.FromSeconds(10);

                var payload = new
                {
                    personalizations = new[] { new { to = new[] { new { email = toEmail } } } },
                    from = new { email = "noreply@tredingsystem.com", name = "TredingSystem" },
                    subject = subject,
                    content = new[] { new { type = "text/html", value = htmlBody } }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via SendGrid to: {Email}", toEmail);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ SendGrid API error: {Status} - {Error}", response.StatusCode, errorContent);
                    throw new Exception($"SendGrid API error: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email via SendGrid to: {Email}", toEmail);
            throw;
        }
    }
}

/// <summary>
/// Resend - Free: 100 emails/day + generous free tier
/// Best for: Modern, developer-friendly, great documentation
/// </summary>
public class ResendEmailProvider : IEmailProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailProvider> _logger;
    private const string ApiUrl = "https://api.resend.com/emails";

    public ResendEmailProvider(IConfiguration configuration, ILogger<ResendEmailProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["EmailProviders:Resend:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning(
                    "⚠️ Resend API key not configured.\n" +
                    "📧 To enable email delivery:\n" +
                    "   1. Go to https://resend.com\n" +
                    "   2. Sign up (free tier: 100 emails/day)\n" +
                    "   3. Go to API Keys\n" +
                    "   4. Create API key\n" +
                    "   5. Set EmailProviders__Resend__ApiKey on Render");
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.Timeout = TimeSpan.FromSeconds(10);

                var payload = new
                {
                    from = "TredingSystem <noreply@tredingsystem.com>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = htmlBody
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via Resend to: {Email}", toEmail);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Resend API error: {Status} - {Error}", response.StatusCode, errorContent);
                    throw new Exception($"Resend API error: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email via Resend to: {Email}", toEmail);
            throw;
        }
    }
}

/// <summary>
/// Email Service Factory - Uses configured provider or defaults to Brevo
/// </summary>
public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string code);
}

public class EmailService : IEmailService
{
    private readonly IEmailProvider _provider;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var providerName = configuration["EmailProvider:Type"] ?? "Brevo"; // Default to Brevo
        
        _provider = providerName.ToLower() switch
        {
            "brevo" => new BrevoEmailProvider(configuration, logger),
            "mailgun" => new MailgunEmailProvider(configuration, logger),
            "sendgrid" => new SendGridEmailProvider(configuration, logger),
            "resend" => new ResendEmailProvider(configuration, logger),
            _ => new BrevoEmailProvider(configuration, logger) // Default
        };

        _logger.LogInformation("📧 Email service initialized with provider: {Provider}", providerName);
    }

    public async Task SendOtpEmailAsync(string toEmail, string code)
    {
        var subject = "🔐 Your OTP Verification Code - TredingSystem";
        var htmlBody = GenerateOtpEmailBody(code);

        await _provider.SendEmailAsync(toEmail, subject, htmlBody);
    }

    private string GenerateOtpEmailBody(string code)
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
