using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IPhoneOtpService
{
    Task<SendOtpResponse> SendPhoneOtpAsync(string phoneNumber, string countryCode);
    Task<VerifyOtpResponse> VerifyPhoneOtpAsync(string phoneNumber, string countryCode, string code);
}
