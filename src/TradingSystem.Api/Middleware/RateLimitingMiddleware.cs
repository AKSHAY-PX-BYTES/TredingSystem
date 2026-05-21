using System.Collections.Concurrent;

namespace TradingSystem.Api.Middleware;

/// <summary>
/// Simple IP-based rate limiting middleware to prevent brute-force and DDoS attacks.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();
    
    // Configuration
    private const int MaxRequests = 100;          // Max requests per window
    private const int WindowSeconds = 60;          // Window duration
    private const int AuthMaxRequests = 10;        // Stricter for auth endpoints
    private const int AuthWindowSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Stricter limits for authentication endpoints
        var isAuthEndpoint = path.StartsWith("/auth/login") || path.StartsWith("/auth/register");
        var maxRequests = isAuthEndpoint ? AuthMaxRequests : MaxRequests;
        var windowSeconds = isAuthEndpoint ? AuthWindowSeconds : WindowSeconds;
        
        var key = isAuthEndpoint ? $"auth:{clientIp}" : $"api:{clientIp}";
        
        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry());
        
        lock (entry)
        {
            var now = DateTime.UtcNow;
            if (now - entry.WindowStart > TimeSpan.FromSeconds(windowSeconds))
            {
                entry.WindowStart = now;
                entry.RequestCount = 0;
            }
            
            entry.RequestCount++;
            
            if (entry.RequestCount > maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP {ClientIp} on path {Path}", clientIp, path);
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = windowSeconds.ToString();
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync("{\"success\":false,\"error\":\"Too many requests. Please try again later.\"}");
                return;
            }
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded headers (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // Periodic cleanup of expired entries (called on a timer in production)
    public static void CleanupExpiredEntries()
    {
        var expiredKeys = _clients
            .Where(kvp => DateTime.UtcNow - kvp.Value.WindowStart > TimeSpan.FromMinutes(5))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _clients.TryRemove(key, out _);
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}
