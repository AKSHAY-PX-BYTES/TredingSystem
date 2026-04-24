using System.Net.Http.Json;
using Microsoft.JSInterop;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
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
}
