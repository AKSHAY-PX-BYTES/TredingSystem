using System.Net.Http.Json;
using Microsoft.JSInterop;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<bool> RefreshTokenAsync();
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<string?> GetUsernameAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<SendOtpResponse> SendOtpAsync(string email);
    Task<VerifyOtpResponse> VerifyOtpAsync(string email, string code);
    Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);
    Task<bool> CheckUsernameExistsAsync(string username);
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string UserKey = "authUser";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result == null)
                return new LoginResponse { Success = false, Error = "Invalid response from server" };

            if (result.Success && !string.IsNullOrEmpty(result.Token))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey,
                    System.Text.Json.JsonSerializer.Serialize(result.User));
            }

            return result;
        }
        catch (Exception ex)
        {
            return new LoginResponse { Success = false, Error = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return result ?? new RegisterResponse { Success = false, Error = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            return new RegisterResponse { Success = false, Error = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUsernameAsync()
    {
        try
        {
            var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserKey);
            if (!string.IsNullOrEmpty(userJson))
            {
                var user = System.Text.Json.JsonSerializer.Deserialize<TradingSystem.Web.Models.UserInfo>(userJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return user?.Username;
            }
        }
        catch { }
        return null;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var username = await GetUsernameAsync();
            if (string.IsNullOrEmpty(username)) return false;

            var response = await _httpClient.PostAsJsonAsync("/auth/refresh", new { Username = username });
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey,
                    System.Text.Json.JsonSerializer.Serialize(result.User));
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SendOtpResponse> SendOtpAsync(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/send-otp", new { Email = email });
            var result = await response.Content.ReadFromJsonAsync<SendOtpResponse>();

            return result ?? new SendOtpResponse { Success = false, Error = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            return new SendOtpResponse { Success = false, Error = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(string email, string code)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/verify-otp", new { Email = email, Code = code });
            var result = await response.Content.ReadFromJsonAsync<VerifyOtpResponse>();

            return result ?? new VerifyOtpResponse { Success = false, Error = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            return new VerifyOtpResponse { Success = false, Error = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/change-password", request);
            var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
            return result ?? new ChangePasswordResponse { Success = false, Error = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            return new ChangePasswordResponse { Success = false, Error = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task<bool> CheckUsernameExistsAsync(string username)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/auth/check-username/{Uri.EscapeDataString(username)}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UsernameCheckResponse>();
                return result?.Exists ?? false;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private class UsernameCheckResponse
    {
        public bool Exists { get; set; }
    }
}

