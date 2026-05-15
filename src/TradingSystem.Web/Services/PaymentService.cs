using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IPaymentService
{
    Task<ApiResponse<CreatePaymentOrderResponse>?> CreateOrderAsync(CreatePaymentOrderRequest request);
    Task<ApiResponse<VerifyPaymentResponse>?> VerifyPaymentAsync(VerifyPaymentRequest request);
    Task<ApiResponse<List<PaymentHistoryItem>>?> GetHistoryAsync();
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _http;

    public PaymentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<CreatePaymentOrderResponse>?> CreateOrderAsync(CreatePaymentOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/create-order", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<CreatePaymentOrderResponse>>();
    }

    public async Task<ApiResponse<VerifyPaymentResponse>?> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        var response = await _http.PostAsJsonAsync("/payment/verify", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<VerifyPaymentResponse>>();
    }

    public async Task<ApiResponse<List<PaymentHistoryItem>>?> GetHistoryAsync()
    {
        return await _http.GetFromJsonAsync<ApiResponse<List<PaymentHistoryItem>>>("/payment/history");
    }
}
