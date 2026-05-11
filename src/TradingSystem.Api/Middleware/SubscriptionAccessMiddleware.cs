using TradingSystem.Api.Services;

namespace TradingSystem.Api.Middleware;

/// <summary>
/// Middleware that checks if free-tier users have expired their 7-day trial.
/// If expired, returns 403 with upgrade message. Paid users pass through.
/// </summary>
public class SubscriptionAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionAccessMiddleware> _logger;

    // Endpoints that are always accessible (login, register, plans, etc.)
    private static readonly string[] OpenEndpoints = new[]
    {
        "/auth/", "/swagger", "/hubs/", "/subscription/plans",
        "/subscription/status", "/subscription/upgrade",
        "/localization/"
    };

    public SubscriptionAccessMiddleware(RequestDelegate next, ILogger<SubscriptionAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Always allow open endpoints
        if (OpenEndpoints.Any(ep => path.StartsWith(ep)))
        {
            await _next(context);
            return;
        }

        // Only check authenticated users
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var username = context.User.Identity.Name;
            var planClaim = context.User.FindFirst("Plan")?.Value ?? "Free";

            // Paid users always have access
            if (planClaim != "Free")
            {
                await _next(context);
                return;
            }

            // Free users — check trial
            if (!string.IsNullOrEmpty(username))
            {
                var subscriptionService = context.RequestServices.GetRequiredService<ISubscriptionService>();
                var hasAccess = await subscriptionService.HasAccessAsync(username);

                if (!hasAccess)
                {
                    _logger.LogWarning("Trial expired for user {Username}, blocking access", username);
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = "Your 7-day free trial has expired. Please upgrade to continue using the platform.",
                        code = "TRIAL_EXPIRED",
                        upgradeUrl = "/plans"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
