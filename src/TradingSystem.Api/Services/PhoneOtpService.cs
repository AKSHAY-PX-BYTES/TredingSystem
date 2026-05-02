using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public class PhoneOtpService : IPhoneOtpService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhoneOtpService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Random _random = new();

    public PhoneOtpService(AppDbContext context, IConfiguration configuration, ILogger<PhoneOtpService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SendOtpResponse> SendPhoneOtpAsync(string phoneNumber, string countryCode)
    {
        try
        {
            // Clean phone number (digits only)
            var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (cleanPhone.Length < 6 || cleanPhone.Length > 15)
            {
                return new SendOtpResponse { Success = false, Error = "Invalid phone number" };
            }

            // Generate 6-digit OTP
            var code = _random.Next(100000, 999999).ToString();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(10);

            // Store as phone:countryCode+number in the OTP table (reuse email field for phone identifier)
            var phoneIdentifier = $"phone:{countryCode}{cleanPhone}";

            // Delete existing unverified OTPs for this phone
            var existingOtps = await _context.Otps
                .Where(o => o.Email == phoneIdentifier && !o.IsVerified)
                .ToListAsync();
            _context.Otps.RemoveRange(existingOtps);

            // Save new OTP
            var otp = new OtpEntity
            {
                Email = phoneIdentifier,
                Code = code,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsVerified = false
            };
            _context.Otps.Add(otp);
            await _context.SaveChangesAsync();

            // Send SMS via Fast2SMS
            var sent = await SendSmsAsync(cleanPhone, countryCode, code);

            if (!sent)
            {
                return new SendOtpResponse { Success = false, Error = "Failed to send SMS. Please try again." };
            }

            _logger.LogInformation("📱 Phone OTP sent to {CountryCode}{Phone}", countryCode, MaskPhone(cleanPhone));

            return new SendOtpResponse
            {
                Success = true,
                Message = "OTP sent to your phone number",
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending phone OTP");
            return new SendOtpResponse { Success = false, Error = "Failed to send OTP. Please try again." };
        }
    }

    public async Task<VerifyOtpResponse> VerifyPhoneOtpAsync(string phoneNumber, string countryCode, string code)
    {
        try
        {
            var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
            var phoneIdentifier = $"phone:{countryCode}{cleanPhone}";
            code = code.Trim();

            var otp = await _context.Otps
                .Where(o => o.Email == phoneIdentifier && o.Code == code && !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return new VerifyOtpResponse { Success = false, Error = "Invalid OTP code" };
            }

            if (DateTime.UtcNow > otp.ExpiresAt)
            {
                return new VerifyOtpResponse { Success = false, Error = "OTP has expired. Please request a new one." };
            }

            // Mark verified
            otp.IsVerified = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Phone verified: {CountryCode}{Phone}", countryCode, MaskPhone(cleanPhone));

            return new VerifyOtpResponse { Success = true, Message = "Phone number verified successfully!" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying phone OTP");
            return new VerifyOtpResponse { Success = false, Error = "Verification failed. Please try again." };
        }
    }

    private async Task<bool> SendSmsAsync(string phoneNumber, string countryCode, string otp)
    {
        var providerType = _configuration["SmsProvider:Type"] ?? "Fast2SMS";

        return providerType.ToLower() switch
        {
            "fast2sms" => await SendViaFast2SmsAsync(phoneNumber, otp),
            _ => await SendViaFast2SmsAsync(phoneNumber, otp)
        };
    }

    /// <summary>
    /// Fast2SMS — Free tier: 100 SMS/day for Indian numbers (+91)
    /// Sign up at: https://www.fast2sms.com/
    /// </summary>
    private async Task<bool> SendViaFast2SmsAsync(string phoneNumber, string otp)
    {
        var apiKey = _configuration["SmsProvider:Fast2SMS:ApiKey"]
                     ?? Environment.GetEnvironmentVariable("FAST2SMS_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("❌ Fast2SMS API key not configured. Set SmsProvider:Fast2SMS:ApiKey or FAST2SMS_API_KEY env var");
            throw new InvalidOperationException("SMS provider not configured. Set FAST2SMS_API_KEY environment variable.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("authorization", apiKey);

            // Fast2SMS OTP route — simplest & free
            var url = $"https://www.fast2sms.com/dev/bulkV2?authorization={apiKey}&route=otp&variables_values={otp}&flash=0&numbers={phoneNumber}";

            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Fast2SMS response: {Status} - {Body}", response.StatusCode, body);

            if (response.IsSuccessStatusCode && body.Contains("\"return\":true"))
            {
                return true;
            }

            _logger.LogError("❌ Fast2SMS failed: {Body}", body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fast2SMS exception");
            return false;
        }
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 4 ? new string('*', phone.Length - 4) + phone[^4..] : "****";
}
