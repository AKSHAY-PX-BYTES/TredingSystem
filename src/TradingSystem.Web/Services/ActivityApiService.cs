using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IActivityApiService
{
    Task<ActivityStatsModel?> GetStatsAsync(int days = 30);
    Task<List<ActivityTimelineModel>?> GetTimelineAsync(int days = 30);
    Task<List<CountryStatsModel>?> GetCountriesAsync(int days = 30);
    Task<List<DeviceStatsModel>?> GetDevicesAsync(int days = 30);
    Task<List<ActivityLogModel>?> GetRecentAsync(int count = 50);
    Task<List<ActivityLogModel>?> SearchAsync(string? eventType = null, string? username = null, string? country = null, int page = 1);
}

public class ActivityApiService : IActivityApiService
{
    private readonly HttpClient _http;

    public ActivityApiService(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public async Task<ActivityStatsModel?> GetStatsAsync(int days = 30)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ApiResponse<ActivityStatsModel>>($"admin/activity/stats?days={days}");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<ActivityTimelineModel>?> GetTimelineAsync(int days = 30)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ApiResponse<List<ActivityTimelineModel>>>($"admin/activity/timeline?days={days}");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<CountryStatsModel>?> GetCountriesAsync(int days = 30)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ApiResponse<List<CountryStatsModel>>>($"admin/activity/countries?days={days}");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<DeviceStatsModel>?> GetDevicesAsync(int days = 30)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ApiResponse<List<DeviceStatsModel>>>($"admin/activity/devices?days={days}");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<ActivityLogModel>?> GetRecentAsync(int count = 50)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<ApiResponse<List<ActivityLogModel>>>($"admin/activity/recent?count={count}");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<ActivityLogModel>?> SearchAsync(string? eventType = null, string? username = null, string? country = null, int page = 1)
    {
        try
        {
            var query = $"admin/activity/search?page={page}";
            if (!string.IsNullOrEmpty(eventType)) query += $"&eventType={eventType}";
            if (!string.IsNullOrEmpty(username)) query += $"&username={username}";
            if (!string.IsNullOrEmpty(country)) query += $"&country={country}";
            var resp = await _http.GetFromJsonAsync<ApiResponse<List<ActivityLogModel>>>(query);
            return resp?.Data;
        }
        catch { return null; }
    }
}

// ── Frontend Models ─────────────────────────────────────

public class ActivityStatsModel
{
    public int TotalEvents { get; set; }
    public int TotalLogins { get; set; }
    public int TotalSignups { get; set; }
    public int FailedLogins { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueIps { get; set; }
    public int UniqueCountries { get; set; }
    public Dictionary<string, int> EventBreakdown { get; set; } = new();
    public Dictionary<string, int> TopCountries { get; set; } = new();
    public Dictionary<string, int> TopCities { get; set; } = new();
    public Dictionary<string, int> BrowserBreakdown { get; set; } = new();
    public Dictionary<string, int> OsBreakdown { get; set; } = new();
    public int DaysCovered { get; set; }
}

public class ActivityTimelineModel
{
    public string Date { get; set; } = string.Empty;
    public int Logins { get; set; }
    public int Signups { get; set; }
    public int Failures { get; set; }
    public int Total { get; set; }
}

public class CountryStatsModel
{
    public string CountryCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Count { get; set; }
    public int UniqueUsers { get; set; }
}

public class DeviceStatsModel
{
    public string DeviceType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ActivityLogModel
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
    public string DeviceType { get; set; } = "Unknown";
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public bool IsSuccess { get; set; }
    public string? Details { get; set; }
    public string? RequestPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
