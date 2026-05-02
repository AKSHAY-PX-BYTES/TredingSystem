using System.Text.Json;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;

namespace TradingSystem.Api.Services;

public interface IActivityTrackingService
{
    Task TrackAsync(ActivityEvent evt);
    Task<List<ActivityLogEntity>> GetActivitiesAsync(ActivityQuery query);
    Task<ActivityStats> GetStatsAsync(int days = 30);
    Task<List<CountryStats>> GetCountryStatsAsync(int days = 30);
    Task<List<ActivityTimeline>> GetTimelineAsync(int days = 30);
    Task<List<DeviceStats>> GetDeviceStatsAsync(int days = 30);
    Task<List<ActivityLogEntity>> GetRecentAsync(int count = 50);
    Task<UserActivitySummary> GetUserActivityAsync(string username, int days = 30);
}

/// <summary>Input DTO to record an activity event.</summary>
public class ActivityEvent
{
    public string EventType { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? Details { get; set; }
}

/// <summary>Query filters for fetching activities.</summary>
public class ActivityQuery
{
    public string? EventType { get; set; }
    public string? Username { get; set; }
    public string? Country { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? IsSuccess { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ── Response DTOs ────────────────────────────────────────────────

public class ActivityStats
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

public class CountryStats
{
    public string CountryCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Count { get; set; }
    public int UniqueUsers { get; set; }
}

public class ActivityTimeline
{
    public string Date { get; set; } = string.Empty;
    public int Logins { get; set; }
    public int Signups { get; set; }
    public int Failures { get; set; }
    public int Total { get; set; }
}

public class DeviceStats
{
    public string DeviceType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class UserActivitySummary
{
    public string Username { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int Logins { get; set; }
    public int FailedLogins { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? LastIp { get; set; }
    public string? LastCountry { get; set; }
    public List<string> KnownIps { get; set; } = new();
    public List<string> KnownCountries { get; set; } = new();
}

// ── Implementation ────────────────────────────────────────────────

public class ActivityTrackingService : IActivityTrackingService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ActivityTrackingService> _logger;

    // Simple in-memory geo cache to avoid hammering the free API
    private static readonly Dictionary<string, GeoResult> _geoCache = new();
    private static readonly object _geoCacheLock = new();

    public ActivityTrackingService(
        AppDbContext db,
        IHttpContextAccessor httpContext,
        IHttpClientFactory httpClientFactory,
        ILogger<ActivityTrackingService> logger)
    {
        _db = db;
        _httpContext = httpContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task TrackAsync(ActivityEvent evt)
    {
        try
        {
            var context = _httpContext.HttpContext;
            var ip = GetClientIp(context);
            var userAgent = context?.Request.Headers["User-Agent"].FirstOrDefault() ?? "";

            var entity = new ActivityLogEntity
            {
                EventType = evt.EventType,
                Username = evt.Username,
                Email = evt.Email,
                IsSuccess = evt.IsSuccess,
                Details = evt.Details,
                IpAddress = ip,
                UserAgent = userAgent,
                HttpMethod = context?.Request.Method,
                RequestPath = context?.Request.Path.Value,
                SessionId = context?.TraceIdentifier,
                CreatedAt = DateTime.UtcNow
            };

            // Parse user agent
            ParseUserAgent(userAgent, entity);

            // Geo-locate IP (fire and forget for speed, update later)
            var geo = await GetGeoLocationAsync(ip);
            if (geo != null)
            {
                entity.Country = geo.Country;
                entity.City = geo.City;
                entity.Region = geo.Region;
                entity.CountryCode = geo.CountryCode;
                entity.Latitude = geo.Latitude;
                entity.Longitude = geo.Longitude;
                entity.Isp = geo.Isp;
                entity.Timezone = geo.Timezone;
            }

            _db.ActivityLogs.Add(entity);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Activity tracked: {EventType} by {User} from {IP} ({Country})",
                evt.EventType, evt.Username ?? "anonymous", ip, entity.Country ?? "unknown");
        }
        catch (Exception ex)
        {
            // Never let tracking failures break the main flow
            _logger.LogWarning(ex, "Failed to track activity: {EventType}", evt.EventType);
        }
    }

    public async Task<List<ActivityLogEntity>> GetActivitiesAsync(ActivityQuery query)
    {
        var q = _db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrEmpty(query.EventType))
            q = q.Where(a => a.EventType == query.EventType);
        if (!string.IsNullOrEmpty(query.Username))
            q = q.Where(a => a.Username == query.Username);
        if (!string.IsNullOrEmpty(query.Country))
            q = q.Where(a => a.Country == query.Country);
        if (query.From.HasValue)
            q = q.Where(a => a.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(a => a.CreatedAt <= query.To.Value);
        if (query.IsSuccess.HasValue)
            q = q.Where(a => a.IsSuccess == query.IsSuccess.Value);

        return await Task.FromResult(
            q.OrderByDescending(a => a.CreatedAt)
             .Skip((query.Page - 1) * query.PageSize)
             .Take(query.PageSize)
             .ToList());
    }

    public async Task<ActivityStats> GetStatsAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var activities = _db.ActivityLogs.Where(a => a.CreatedAt >= since).ToList();

        return await Task.FromResult(new ActivityStats
        {
            TotalEvents = activities.Count,
            TotalLogins = activities.Count(a => a.EventType == "Login" && a.IsSuccess),
            TotalSignups = activities.Count(a => a.EventType == "Signup" && a.IsSuccess),
            FailedLogins = activities.Count(a => a.EventType == "Login" && !a.IsSuccess),
            UniqueUsers = activities.Where(a => a.Username != null).Select(a => a.Username).Distinct().Count(),
            UniqueIps = activities.Select(a => a.IpAddress).Distinct().Count(),
            UniqueCountries = activities.Where(a => a.Country != null).Select(a => a.Country).Distinct().Count(),
            EventBreakdown = activities.GroupBy(a => a.EventType)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopCountries = activities.Where(a => a.Country != null)
                .GroupBy(a => a.Country!).OrderByDescending(g => g.Count()).Take(10)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopCities = activities.Where(a => a.City != null)
                .GroupBy(a => a.City!).OrderByDescending(g => g.Count()).Take(10)
                .ToDictionary(g => g.Key, g => g.Count()),
            BrowserBreakdown = activities.Where(a => a.Browser != null)
                .GroupBy(a => a.Browser!).OrderByDescending(g => g.Count()).Take(8)
                .ToDictionary(g => g.Key, g => g.Count()),
            OsBreakdown = activities.Where(a => a.OperatingSystem != null)
                .GroupBy(a => a.OperatingSystem!).OrderByDescending(g => g.Count()).Take(8)
                .ToDictionary(g => g.Key, g => g.Count()),
            DaysCovered = days
        });
    }

    public async Task<List<CountryStats>> GetCountryStatsAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var activities = _db.ActivityLogs.Where(a => a.CreatedAt >= since && a.Country != null).ToList();

        return await Task.FromResult(
            activities.GroupBy(a => new { a.CountryCode, a.Country })
                .Select(g => new CountryStats
                {
                    CountryCode = g.Key.CountryCode ?? "",
                    Country = g.Key.Country ?? "",
                    Count = g.Count(),
                    UniqueUsers = g.Where(a => a.Username != null).Select(a => a.Username).Distinct().Count()
                })
                .OrderByDescending(c => c.Count)
                .ToList());
    }

    public async Task<List<ActivityTimeline>> GetTimelineAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var activities = _db.ActivityLogs.Where(a => a.CreatedAt >= since).ToList();

        var grouped = activities.GroupBy(a => a.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new ActivityTimeline
            {
                Date = g.Key,
                Logins = g.Count(a => a.EventType == "Login" && a.IsSuccess),
                Signups = g.Count(a => a.EventType == "Signup" && a.IsSuccess),
                Failures = g.Count(a => !a.IsSuccess),
                Total = g.Count()
            }).ToList();

        // Fill in missing dates
        var result = new List<ActivityTimeline>();
        for (int i = 0; i < days; i++)
        {
            var date = since.AddDays(i + 1).ToString("yyyy-MM-dd");
            var existing = grouped.FirstOrDefault(g => g.Date == date);
            result.Add(existing ?? new ActivityTimeline { Date = date });
        }

        return await Task.FromResult(result);
    }

    public async Task<List<DeviceStats>> GetDeviceStatsAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var activities = _db.ActivityLogs.Where(a => a.CreatedAt >= since).ToList();
        var total = activities.Count;

        return await Task.FromResult(
            activities.GroupBy(a => a.DeviceType)
                .Select(g => new DeviceStats
                {
                    DeviceType = g.Key,
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round(g.Count() * 100.0 / total, 1) : 0
                })
                .OrderByDescending(d => d.Count)
                .ToList());
    }

    public async Task<List<ActivityLogEntity>> GetRecentAsync(int count = 50)
    {
        return await Task.FromResult(
            _db.ActivityLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList());
    }

    public async Task<UserActivitySummary> GetUserActivityAsync(string username, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var activities = _db.ActivityLogs
            .Where(a => a.Username == username && a.CreatedAt >= since)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        var lastLogin = activities.FirstOrDefault(a => a.EventType == "Login" && a.IsSuccess);

        return await Task.FromResult(new UserActivitySummary
        {
            Username = username,
            TotalEvents = activities.Count,
            Logins = activities.Count(a => a.EventType == "Login" && a.IsSuccess),
            FailedLogins = activities.Count(a => a.EventType == "Login" && !a.IsSuccess),
            LastLogin = lastLogin?.CreatedAt,
            LastIp = lastLogin?.IpAddress,
            LastCountry = lastLogin?.Country,
            KnownIps = activities.Select(a => a.IpAddress).Distinct().Take(20).ToList(),
            KnownCountries = activities.Where(a => a.Country != null).Select(a => a.Country!).Distinct().ToList()
        });
    }

    // ── Private helpers ─────────────────────────────────────────

    private static string GetClientIp(HttpContext? context)
    {
        if (context == null) return "unknown";

        // Check common proxy headers first
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            var ip = forwarded.Split(',').First().Trim();
            if (!string.IsNullOrEmpty(ip)) return ip;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static void ParseUserAgent(string ua, ActivityLogEntity entity)
    {
        if (string.IsNullOrEmpty(ua))
        {
            entity.DeviceType = "Unknown";
            return;
        }

        var uaLower = ua.ToLowerInvariant();

        // Device type
        if (uaLower.Contains("mobile") || uaLower.Contains("android") || uaLower.Contains("iphone"))
            entity.DeviceType = "Mobile";
        else if (uaLower.Contains("tablet") || uaLower.Contains("ipad"))
            entity.DeviceType = "Tablet";
        else if (uaLower.Contains("bot") || uaLower.Contains("spider") || uaLower.Contains("crawl"))
            entity.DeviceType = "Bot";
        else
            entity.DeviceType = "Desktop";

        // Browser
        if (uaLower.Contains("edg/") || uaLower.Contains("edge"))
            entity.Browser = "Edge";
        else if (uaLower.Contains("chrome") && !uaLower.Contains("chromium"))
            entity.Browser = "Chrome";
        else if (uaLower.Contains("firefox"))
            entity.Browser = "Firefox";
        else if (uaLower.Contains("safari") && !uaLower.Contains("chrome"))
            entity.Browser = "Safari";
        else if (uaLower.Contains("opera") || uaLower.Contains("opr/"))
            entity.Browser = "Opera";
        else
            entity.Browser = "Other";

        // OS
        if (uaLower.Contains("windows"))
            entity.OperatingSystem = "Windows";
        else if (uaLower.Contains("mac os") || uaLower.Contains("macintosh"))
            entity.OperatingSystem = "macOS";
        else if (uaLower.Contains("linux") && !uaLower.Contains("android"))
            entity.OperatingSystem = "Linux";
        else if (uaLower.Contains("android"))
            entity.OperatingSystem = "Android";
        else if (uaLower.Contains("iphone") || uaLower.Contains("ipad") || uaLower.Contains("ios"))
            entity.OperatingSystem = "iOS";
        else
            entity.OperatingSystem = "Other";
    }

    private async Task<GeoResult?> GetGeoLocationAsync(string ip)
    {
        // Skip for local/private IPs
        if (ip == "unknown" || ip == "::1" || ip.StartsWith("127.") || ip.StartsWith("10.") ||
            ip.StartsWith("192.168.") || ip.StartsWith("172."))
            return new GeoResult { Country = "Local", City = "Local", CountryCode = "LO", Region = "Local" };

        // Check cache
        lock (_geoCacheLock)
        {
            if (_geoCache.TryGetValue(ip, out var cached))
                return cached;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // Free IP geolocation API (no key required, 45 requests/minute)
            var response = await client.GetAsync($"http://ip-api.com/json/{ip}?fields=status,country,countryCode,regionName,city,lat,lon,timezone,isp");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var status) && status.GetString() == "success")
                {
                    var result = new GeoResult
                    {
                        Country = GetString(root, "country"),
                        CountryCode = GetString(root, "countryCode"),
                        Region = GetString(root, "regionName"),
                        City = GetString(root, "city"),
                        Latitude = GetDouble(root, "lat"),
                        Longitude = GetDouble(root, "lon"),
                        Timezone = GetString(root, "timezone"),
                        Isp = GetString(root, "isp")
                    };

                    lock (_geoCacheLock)
                    {
                        _geoCache[ip] = result;
                        // Keep cache manageable
                        if (_geoCache.Count > 10000)
                            _geoCache.Clear();
                    }

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to geo-locate IP {IP}", ip);
        }

        return null;
    }

    private static string? GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static double? GetDouble(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetDouble() : null;

    private class GeoResult
    {
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Timezone { get; set; }
        public string? Isp { get; set; }
    }
}
