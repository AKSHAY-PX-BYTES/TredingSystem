namespace TradingSystem.Api.Data.Entities;

public class ActivityLogEntity
{
    public long Id { get; set; }
    
    /// <summary>Event type: Login, Signup, Logout, OtpSent, OtpVerified, TokenRefresh, FeatureToggle, StockSearch, PasswordReset, etc.</summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>Username of the user who triggered the event (null for anonymous actions)</summary>
    public string? Username { get; set; }
    
    /// <summary>Email associated with the event</summary>
    public string? Email { get; set; }
    
    /// <summary>Client IP address (supports IPv4 and IPv6)</summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>User-Agent header from the request</summary>
    public string? UserAgent { get; set; }
    
    /// <summary>Country resolved from IP geolocation</summary>
    public string? Country { get; set; }
    
    /// <summary>City resolved from IP geolocation</summary>
    public string? City { get; set; }
    
    /// <summary>Region/State resolved from IP geolocation</summary>
    public string? Region { get; set; }
    
    /// <summary>ISO country code (e.g., "IN", "US")</summary>
    public string? CountryCode { get; set; }
    
    /// <summary>Latitude from geolocation</summary>
    public double? Latitude { get; set; }
    
    /// <summary>Longitude from geolocation</summary>
    public double? Longitude { get; set; }
    
    /// <summary>ISP / Organization name</summary>
    public string? Isp { get; set; }
    
    /// <summary>Timezone string (e.g., "Asia/Kolkata")</summary>
    public string? Timezone { get; set; }
    
    /// <summary>Device type parsed from User-Agent: Desktop, Mobile, Tablet, Bot, Unknown</summary>
    public string DeviceType { get; set; } = "Unknown";
    
    /// <summary>Browser name parsed from User-Agent</summary>
    public string? Browser { get; set; }
    
    /// <summary>OS name parsed from User-Agent</summary>
    public string? OperatingSystem { get; set; }
    
    /// <summary>Whether the event was successful (e.g., login success vs failure)</summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>Optional details or failure reason</summary>
    public string? Details { get; set; }
    
    /// <summary>HTTP method used (GET, POST, etc.)</summary>
    public string? HttpMethod { get; set; }
    
    /// <summary>Request path (e.g., /auth/login)</summary>
    public string? RequestPath { get; set; }
    
    /// <summary>Session or correlation ID for grouping related events</summary>
    public string? SessionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
