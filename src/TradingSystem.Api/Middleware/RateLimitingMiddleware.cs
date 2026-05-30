using System.Collections.Concurrent;

namespace TradingSystem.Api.Middleware;

/// <summary>
/// Advanced IP-based rate limiting middleware with per-endpoint granularity.
/// Prevents brute-force attacks, credential stuffing, and API abuse.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    // Rate limit tiers
    private const int GeneralMaxRequests = 120;       // General API: 120 req/min
    private const int GeneralWindowSeconds = 60;
    
    private const int AuthMaxRequests = 5;            // Login/Register: 5 req/min (brute-force protection)
    private const int AuthWindowSeconds = 60;
    
    private const int OtpMaxRequests = 3;             // OTP endpoints: 3 req/min (SMS abuse prevention)
    private const int OtpWindowSeconds = 60;
    
    private const int SensitiveMaxRequests = 10;      // Password reset, token refresh: 10 req/min
    private const int SensitiveWindowSeconds = 60;
    
    private const int CheckUsernameMax = 15;          // Username check: 15 req/min (enumeration prevention)
    private const int CheckUsernameWindow = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        // Determine rate limit tier based on endpoint
        var (maxRequests, windowSeconds, tierKey) = ClassifyEndpoint(path, method);
        
        var key = $"{tierKey}:{clientIp}";
        
        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry());
        
        bool rateLimited = false;
        
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
                _logger.LogWarning("Rate limit [{Tier}] exceeded for IP {ClientIp} on {Method} {Path} ({Count}/{Max})",
                    tierKey, clientIp, method, path, entry.RequestCount, maxRequests);
                rateLimited = true;
            }
            
            // Add rate limit headers for transparency
            if (!rateLimited)
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
                    context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, maxRequests - entry.RequestCount).ToString();
                    return Task.CompletedTask;
                });
            }
        }

        if (rateLimited)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = windowSeconds.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"error\":\"Too many requests. Please slow down and try again later.\"}");
            return;
        }

        // Periodic cleanup (every 5 minutes)
        if ((DateTime.UtcNow - _lastCleanup).TotalMinutes > 5)
        {
            _lastCleanup = DateTime.UtcNow;
            _ = Task.Run(CleanupExpiredEntries);
        }

        await _next(context);
    }

    private static (int maxRequests, int windowSeconds, string tierKey) ClassifyEndpoint(string path, string method)
    {
        // Authentication endpoints - strictest limits
        if (path.StartsWith("/auth/login") || path.StartsWith("/auth/register"))
            return (AuthMaxRequests, AuthWindowSeconds, "auth");

        // OTP endpoints - prevent SMS/email abuse
        if (path.Contains("/otp") || path.Contains("/send-otp") || path.Contains("/verify-otp"))
            return (OtpMaxRequests, OtpWindowSeconds, "otp");

        // Username/email enumeration prevention
        if (path.StartsWith("/auth/check-username") || path.StartsWith("/auth/check-email"))
            return (CheckUsernameMax, CheckUsernameWindow, "check");

        // Password reset & token operations
        if (path.Contains("/forgot-password") || path.Contains("/reset-password") || path.Contains("/refresh-token"))
            return (SensitiveMaxRequests, SensitiveWindowSeconds, "sensitive");

        // Payment webhooks - slightly relaxed (trusted source)
        if (path.Contains("/webhook"))
            return (30, 60, "webhook");

        // All other API calls
        return (GeneralMaxRequests, GeneralWindowSeconds, "api");
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded headers (behind proxy/load balancer like Render)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp.Trim();
            
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

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
