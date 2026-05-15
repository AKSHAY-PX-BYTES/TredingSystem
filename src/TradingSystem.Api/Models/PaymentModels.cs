using System.ComponentModel.DataAnnotations;

namespace TradingSystem.Api.Models;

public class CreatePaymentOrderRequest
{
    [Required]
    public string Plan { get; set; } = string.Empty;
    
    public bool IsAnnual { get; set; } = false;
}

public class CreatePaymentOrderResponse
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? RazorpayKeyId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Description { get; set; }
    public string? Error { get; set; }
}

public class VerifyPaymentRequest
{
    [Required]
    public string RazorpayOrderId { get; set; } = string.Empty;

    [Required]
    public string RazorpayPaymentId { get; set; } = string.Empty;

    [Required]
    public string RazorpaySignature { get; set; } = string.Empty;

    [Required]
    public string Plan { get; set; } = string.Empty;

    public bool IsAnnual { get; set; }
}

public class VerifyPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? TransactionId { get; set; }
    public SubscriptionInfo? Subscription { get; set; }
}

public class PaymentHistoryItem
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
