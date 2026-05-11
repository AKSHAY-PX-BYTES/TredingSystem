using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface INotificationApiService
{
    Task<List<NotificationDto>> GetNotificationsAsync(int page = 1);
    Task<int> GetUnreadCountAsync();
    Task MarkAsReadAsync(long id);
    Task MarkAllAsReadAsync();
    Task<List<PriceAlertDto>> GetAlertsAsync();
    Task<PriceAlertDto?> CreateAlertAsync(CreatePriceAlertRequest request);
    Task DeleteAlertAsync(int id);
}

public class NotificationApiService : INotificationApiService
{
    private readonly HttpClient _httpClient;

    public NotificationApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<List<NotificationDto>> GetNotificationsAsync(int page = 1)
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<ApiResponse<List<NotificationDto>>>($"/notifications?page={page}");
            return resp?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<Dictionary<string, int>>("/notifications/unread-count");
            return resp?.GetValueOrDefault("count", 0) ?? 0;
        }
        catch { return 0; }
    }

    public async Task MarkAsReadAsync(long id)
    {
        try { await _httpClient.PostAsync($"/notifications/{id}/read", null); } catch { }
    }

    public async Task MarkAllAsReadAsync()
    {
        try { await _httpClient.PostAsync("/notifications/read-all", null); } catch { }
    }

    public async Task<List<PriceAlertDto>> GetAlertsAsync()
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<ApiResponse<List<PriceAlertDto>>>("/notifications/alerts");
            return resp?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<PriceAlertDto?> CreateAlertAsync(CreatePriceAlertRequest request)
    {
        try
        {
            var resp = await _httpClient.PostAsJsonAsync("/notifications/alerts", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<PriceAlertDto>>();
            return result?.Data;
        }
        catch { return null; }
    }

    public async Task DeleteAlertAsync(int id)
    {
        try { await _httpClient.DeleteAsync($"/notifications/alerts/{id}"); } catch { }
    }
}
