using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IPaymentService
{
    Task<CreatePaymentOrderResponse?> CreateOrderAsync(CreatePaymentOrderRequest request);
    Task<CreatePaymentOrderResponse?> CreateOrderAnonymousAsync(AnonymousCreateOrderRequest request);
    Task<VerifyPaymentResponse?> VerifyPaymentAsync(VerifyPaymentRequest request);
    Task<VerifyPaymentResponse?> VerifyPaymentAnonymousAsync(AnonymousVerifyRequest request);
    Task<ApiResponse<List<PaymentHistoryItem>>?> GetHistoryAsync();
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _http;

    public PaymentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreatePaymentOrderResponse?> CreateOrderAsync(CreatePaymentOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/create-order", request);
        return await response.Content.ReadFromJsonAsync<CreatePaymentOrderResponse>();
    }

    public async Task<CreatePaymentOrderResponse?> CreateOrderAnonymousAsync(AnonymousCreateOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/create-order-anonymous", request);
        return await response.Content.ReadFromJsonAsync<CreatePaymentOrderResponse>();
    }

    public async Task<VerifyPaymentResponse?> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/verify", request);
        return await response.Content.ReadFromJsonAsync<VerifyPaymentResponse>();
    }

    public async Task<VerifyPaymentResponse?> VerifyPaymentAnonymousAsync(AnonymousVerifyRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/verify-anonymous", request);
        return await response.Content.ReadFromJsonAsync<VerifyPaymentResponse>();
    }

    public async Task<ApiResponse<List<PaymentHistoryItem>>?> GetHistoryAsync()
    {
        return await _http.GetFromJsonAsync<ApiResponse<List<PaymentHistoryItem>>>("/payment/history");
    }
}
