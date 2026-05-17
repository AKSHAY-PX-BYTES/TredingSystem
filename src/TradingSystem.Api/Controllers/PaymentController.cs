using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("payment")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IActivityTrackingService _activityTracker;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentController(IPaymentService paymentService, IActivityTrackingService activityTracker, 
        ILogger<PaymentController> logger, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _activityTracker = activityTracker;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Check Razorpay configuration status (for debugging)
    /// </summary>
    [HttpGet("config-check")]
    [AllowAnonymous]
    public IActionResult ConfigCheck()
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];
        var webhookSecret = _configuration["Razorpay:WebhookSecret"];
        
        return Ok(new
        {
            keyIdSet = !string.IsNullOrEmpty(keyId),
            keyIdPrefix = keyId?.Length > 8 ? keyId[..8] + "..." : keyId ?? "NOT SET",
            keySecretSet = !string.IsNullOrEmpty(keySecret),
            keySecretLength = keySecret?.Length ?? 0,
            webhookSecretSet = !string.IsNullOrEmpty(webhookSecret),
            currency = _configuration["Razorpay:Currency"] ?? "INR",
            companyName = _configuration["Razorpay:CompanyName"] ?? "NOT SET",
            plans = new[] { "Pro (₹799/mo)", "Premium (₹1599/mo)", "Enterprise (₹39999/mo)" }
        });
    }

    /// <summary>
    /// Create a Razorpay payment order for plan upgrade
    /// </summary>
    [HttpPost("create-order")]
    [ProducesResponseType(typeof(CreatePaymentOrderResponse), 200)]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new CreatePaymentOrderResponse { Success = false, Error = "Not authenticated" });

        _logger.LogInformation("POST /payment/create-order for user {Username}, plan {Plan}", username, request.Plan);

        if (!ModelState.IsValid)
            return BadRequest(new CreatePaymentOrderResponse { Success = false, Error = "Invalid request" });

        var result = await _paymentService.CreateOrderAsync(username, request);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "PaymentOrderCreated",
            Username = username,
            IsSuccess = result.Success,
            Details = result.Success ? $"Order {result.OrderId} for {request.Plan}" : result.Error
        });

        return Ok(result);
    }

    /// <summary>
    /// Verify payment after Razorpay checkout completion
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyPaymentResponse), 200)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new VerifyPaymentResponse { Success = false, Error = "Not authenticated" });

        _logger.LogInformation("POST /payment/verify for user {Username}, order {OrderId}", username, request.RazorpayOrderId);

        if (!ModelState.IsValid)
            return BadRequest(new VerifyPaymentResponse { Success = false, Error = "Invalid request" });

        var result = await _paymentService.VerifyPaymentAsync(username, request);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = result.Success ? "PaymentSuccess" : "PaymentFailed",
            Username = username,
            IsSuccess = result.Success,
            Details = result.Success ? $"Paid for {request.Plan} - {result.TransactionId}" : result.Error
        });

        return Ok(result);
    }

    /// <summary>
    /// Get payment history for the authenticated user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentHistoryItem>>), 200)]
    public async Task<IActionResult> GetHistory()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(ApiResponse<List<PaymentHistoryItem>>.Fail("Not authenticated"));

        var history = await _paymentService.GetPaymentHistoryAsync(username);
        return Ok(ApiResponse<List<PaymentHistoryItem>>.Ok(history));
    }

    /// <summary>
    /// Razorpay webhook endpoint (server-to-server, no auth required)
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? "";

        _logger.LogInformation("Razorpay webhook received");

        var success = await _paymentService.HandleWebhookAsync(payload, signature);

        return success ? Ok("OK") : BadRequest("Invalid signature");
    }

    /// <summary>
    /// Create a Razorpay order for signup (anonymous, no auth needed)
    /// </summary>
    [HttpPost("create-order-anonymous")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrderAnonymous([FromBody] AnonymousCreateOrderRequest request)
    {
        _logger.LogInformation("POST /payment/create-order-anonymous for email {Email}, plan {Plan}", request.Email, request.Plan);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Plan))
            return BadRequest(new CreatePaymentOrderResponse { Success = false, Error = "Email and Plan are required." });

        var result = await _paymentService.CreateOrderAnonymousAsync(request.Email, new CreatePaymentOrderRequest
        {
            Plan = request.Plan,
            IsAnnual = request.IsAnnual
        });

        return Ok(result);
    }

    /// <summary>
    /// Verify a Razorpay payment for signup (anonymous, no auth needed)
    /// </summary>
    [HttpPost("verify-anonymous")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPaymentAnonymous([FromBody] AnonymousVerifyRequest request)
    {
        _logger.LogInformation("POST /payment/verify-anonymous for order {OrderId}", request.RazorpayOrderId);

        if (string.IsNullOrWhiteSpace(request.RazorpayOrderId) || string.IsNullOrWhiteSpace(request.RazorpayPaymentId))
            return BadRequest(new VerifyPaymentResponse { Success = false, Error = "Invalid request." });

        var result = await _paymentService.VerifyPaymentAnonymousAsync(
            request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);

        return Ok(result);
    }
}
