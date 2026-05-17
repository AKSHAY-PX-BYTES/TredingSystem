using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IPaymentService
{
    Task<CreatePaymentOrderResponse> CreateOrderAsync(string username, CreatePaymentOrderRequest request);
    Task<CreatePaymentOrderResponse> CreateOrderAnonymousAsync(string email, CreatePaymentOrderRequest request);
    Task<VerifyPaymentResponse> VerifyPaymentAsync(string username, VerifyPaymentRequest request);
    Task<VerifyPaymentResponse> VerifyPaymentAnonymousAsync(string orderId, string paymentId, string signature);
    Task<List<PaymentHistoryItem>> GetPaymentHistoryAsync(string username);
    Task<bool> HandleWebhookAsync(string payload, string signature);
}

public class RazorpayPaymentService : IPaymentService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RazorpayPaymentService> _logger;
    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, (decimal Monthly, decimal Annual)> PlanPrices = new()
    {
        ["Pro"] = (799m, 6399m),             // ₹799/mo or ₹6399/year (~₹533/mo)
        ["Premium"] = (1599m, 12799m),     // ₹1599/mo or ₹12799/year (~₹1066/mo)
        ["Enterprise"] = (39999m, 399999m) // ₹39999/mo or ₹3,99,999/year
    };

    public RazorpayPaymentService(IServiceScopeFactory scopeFactory, IConfiguration configuration, 
        ILogger<RazorpayPaymentService> logger, IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Razorpay");
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task<CreatePaymentOrderResponse> CreateOrderAsync(string username, CreatePaymentOrderRequest request)
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
        {
            return new CreatePaymentOrderResponse { Success = false, Error = $"Payment gateway not configured. KeyId={(string.IsNullOrEmpty(keyId) ? "MISSING" : "SET")}, KeySecret={(string.IsNullOrEmpty(keySecret) ? "MISSING" : "SET")}" };
        }

        if (!PlanPrices.ContainsKey(request.Plan))
        {
            return new CreatePaymentOrderResponse { Success = false, Error = $"Invalid plan: {request.Plan}" };
        }

        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return new CreatePaymentOrderResponse { Success = false, Error = "User not found." };
        }

        var (monthly, annual) = PlanPrices[request.Plan];
        var amount = request.IsAnnual ? annual : monthly;
        var currency = _configuration["Razorpay:Currency"] ?? "INR";
        var amountInPaise = (int)(amount * 100);

        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var orderPayload = new
            {
                amount = amountInPaise,
                currency,
                receipt = $"rcpt_{user.Id}_{DateTime.UtcNow.Ticks}",
                notes = new
                {
                    plan = request.Plan,
                    username,
                    is_annual = request.IsAnnual.ToString(),
                    user_email = user.Email
                }
            };

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(orderPayload),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay order creation failed: status={Status}, response={Response}", response.StatusCode, responseBody);
                return new CreatePaymentOrderResponse { Success = false, Error = $"Razorpay API error ({response.StatusCode}): {responseBody}" };
            }

            var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var orderId = orderResponse.GetProperty("id").GetString()!;

            // Save payment record
            db.Payments.Add(new PaymentEntity
            {
                UserId = user.Id,
                OrderId = orderId,
                Plan = request.Plan,
                Amount = amount,
                Currency = currency,
                Status = "created",
                IsAnnual = request.IsAnnual,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            _logger.LogInformation("Razorpay order created: {OrderId} for user {Username}, plan {Plan}, amount {Amount}",
                orderId, username, request.Plan, amount);

            return new CreatePaymentOrderResponse
            {
                Success = true,
                OrderId = orderId,
                Amount = amount,
                Currency = currency,
                RazorpayKeyId = keyId,
                CustomerName = user.DisplayName,
                CustomerEmail = user.Email,
                Description = $"{request.Plan} Plan - {(request.IsAnnual ? "Annual" : "Monthly")} Subscription"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order: {Message}", ex.Message);
            return new CreatePaymentOrderResponse { Success = false, Error = $"Payment service error: {ex.Message}" };
        }
    }

    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(string username, VerifyPaymentRequest request)
    {
        var keySecret = _configuration["Razorpay:KeySecret"];
        if (string.IsNullOrEmpty(keySecret))
        {
            return new VerifyPaymentResponse { Success = false, Error = "Payment gateway not configured." };
        }

        // Verify signature
        var payload = $"{request.RazorpayOrderId}|{request.RazorpayPaymentId}";
        var expectedSignature = ComputeHmacSha256(payload, keySecret);

        if (expectedSignature != request.RazorpaySignature)
        {
            _logger.LogWarning("Payment signature mismatch for order {OrderId}", request.RazorpayOrderId);
            return new VerifyPaymentResponse { Success = false, Error = "Payment verification failed. Invalid signature." };
        }

        using var db = CreateDbContext();

        // Update payment record
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.RazorpayOrderId);
        if (payment == null)
        {
            return new VerifyPaymentResponse { Success = false, Error = "Payment order not found." };
        }

        if (payment.Status == "paid")
        {
            return new VerifyPaymentResponse { Success = true, Message = "Payment already verified.", TransactionId = payment.PaymentId };
        }

        payment.PaymentId = request.RazorpayPaymentId;
        payment.Signature = request.RazorpaySignature;
        payment.Status = "paid";
        payment.PaidAt = DateTime.UtcNow;

        // Upgrade user plan
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null)
        {
            user.Plan = request.Plan;
            user.IsTrialUsed = true;
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Payment verified: {PaymentId}, user {Username} upgraded to {Plan}",
            request.RazorpayPaymentId, username, request.Plan);

        return new VerifyPaymentResponse
        {
            Success = true,
            Message = $"Payment successful! You are now on the {request.Plan} plan.",
            TransactionId = request.RazorpayPaymentId,
            Subscription = new SubscriptionInfo
            {
                Plan = request.Plan,
                HasAccess = true,
                StartDate = DateTime.UtcNow,
                EndDate = request.IsAnnual ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1)
            }
        };
    }

    /// <summary>
    /// Create order for anonymous (signup) flow — no user in DB yet
    /// </summary>
    public async Task<CreatePaymentOrderResponse> CreateOrderAnonymousAsync(string email, CreatePaymentOrderRequest request)
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];

        _logger.LogInformation("CreateOrderAnonymous called: email={Email}, plan={Plan}, keyId={KeyId}, keySecretLength={SecretLen}",
            email, request.Plan, keyId ?? "NULL", keySecret?.Length ?? 0);

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
            return new CreatePaymentOrderResponse { Success = false, Error = $"Payment gateway not configured. KeyId={(string.IsNullOrEmpty(keyId) ? "MISSING" : "SET")}, KeySecret={(string.IsNullOrEmpty(keySecret) ? "MISSING" : "SET")}" };

        if (!PlanPrices.ContainsKey(request.Plan))
            return new CreatePaymentOrderResponse { Success = false, Error = $"Invalid plan: {request.Plan}" };

        var (monthly, annual) = PlanPrices[request.Plan];
        var amount = request.IsAnnual ? annual : monthly;
        var currency = _configuration["Razorpay:Currency"] ?? "INR";
        var amountInPaise = (int)(amount * 100);

        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));

            // Use a fresh HttpRequestMessage each time
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var orderPayload = new
            {
                amount = amountInPaise,
                currency,
                receipt = $"signup_{DateTime.UtcNow.Ticks}",
                notes = new { plan = request.Plan, email, is_annual = request.IsAnnual.ToString(), flow = "signup" }
            };

            var jsonPayload = JsonSerializer.Serialize(orderPayload);
            httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Razorpay API: POST /v1/orders, payload={Payload}", jsonPayload);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Razorpay API response: status={Status}, body={Body}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay anonymous order creation failed: status={Status}, response={Response}", response.StatusCode, responseBody);
                return new CreatePaymentOrderResponse { Success = false, Error = $"Razorpay API error ({response.StatusCode}): {responseBody}" };
            }

            var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var orderId = orderResponse.GetProperty("id").GetString()!;

            // Save payment record with UserId=null (will be linked after registration)
            using var db = CreateDbContext();
            db.Payments.Add(new PaymentEntity
            {
                UserId = null,
                OrderId = orderId,
                Plan = request.Plan,
                Amount = amount,
                Currency = currency,
                Status = "created",
                IsAnnual = request.IsAnnual,
                CreatedAt = DateTime.UtcNow,
                PaymentMethod = email // store email temporarily to link later
            });
            await db.SaveChangesAsync();

            _logger.LogInformation("Anonymous Razorpay order created: {OrderId} for email {Email}, plan {Plan}", orderId, email, request.Plan);

            return new CreatePaymentOrderResponse
            {
                Success = true,
                OrderId = orderId,
                Amount = amount,
                Currency = currency,
                RazorpayKeyId = keyId,
                CustomerEmail = email,
                Description = $"{request.Plan} Plan - {(request.IsAnnual ? "Annual" : "Monthly")} Subscription"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateOrderAnonymousAsync: {Message}", ex.Message);
            return new CreatePaymentOrderResponse { Success = false, Error = $"Payment service error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Verify payment for anonymous (signup) flow — just validates signature + marks as paid
    /// </summary>
    public async Task<VerifyPaymentResponse> VerifyPaymentAnonymousAsync(string orderId, string paymentId, string signature)
    {
        var keySecret = _configuration["Razorpay:KeySecret"];
        if (string.IsNullOrEmpty(keySecret))
            return new VerifyPaymentResponse { Success = false, Error = "Payment gateway not configured." };

        var payload = $"{orderId}|{paymentId}";
        var expectedSignature = ComputeHmacSha256(payload, keySecret);

        if (expectedSignature != signature)
        {
            _logger.LogWarning("Anonymous payment signature mismatch for order {OrderId}", orderId);
            return new VerifyPaymentResponse { Success = false, Error = "Payment verification failed. Invalid signature." };
        }

        using var db = CreateDbContext();
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (payment == null)
            return new VerifyPaymentResponse { Success = false, Error = "Payment order not found." };

        if (payment.Status == "paid")
            return new VerifyPaymentResponse { Success = true, Message = "Payment already verified.", TransactionId = paymentId };

        payment.PaymentId = paymentId;
        payment.Signature = signature;
        payment.Status = "paid";
        payment.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Anonymous payment verified: {PaymentId}, order {OrderId}", paymentId, orderId);

        return new VerifyPaymentResponse { Success = true, Message = "Payment verified successfully.", TransactionId = paymentId };
    }

    public async Task<List<PaymentHistoryItem>> GetPaymentHistoryAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return new();

        return await db.Payments
            .Where(p => p.UserId == user.Id && p.Status == "paid")
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentHistoryItem
            {
                TransactionId = p.PaymentId ?? "",
                OrderId = p.OrderId,
                Plan = p.Plan,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod ?? "Razorpay",
                CreatedAt = p.PaidAt ?? p.CreatedAt
            })
            .Take(50)
            .ToListAsync();
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        var webhookSecret = _configuration["Razorpay:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogWarning("Razorpay webhook secret not configured");
            return false;
        }

        // Verify webhook signature
        var expectedSignature = ComputeHmacSha256(payload, webhookSecret);
        if (expectedSignature != signature)
        {
            _logger.LogWarning("Webhook signature mismatch");
            return false;
        }

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(payload);
            var eventType = data.GetProperty("event").GetString();

            _logger.LogInformation("Razorpay webhook received: {Event}", eventType);

            if (eventType == "payment.captured")
            {
                var paymentEntity = data.GetProperty("payload").GetProperty("payment").GetProperty("entity");
                var orderId = paymentEntity.GetProperty("order_id").GetString();
                var method = paymentEntity.GetProperty("method").GetString();

                using var db = CreateDbContext();
                var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
                if (payment != null && payment.Status != "paid")
                {
                    payment.Status = "paid";
                    payment.PaymentMethod = method;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.RawResponse = payload;
                    await db.SaveChangesAsync();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return false;
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
