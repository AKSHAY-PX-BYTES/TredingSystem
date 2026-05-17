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
        try
        {
            var response = await _http.PostAsJsonAsync("/payment/create-order", request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var errorResult = System.Text.Json.JsonSerializer.Deserialize<CreatePaymentOrderResponse>(body,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return errorResult ?? new CreatePaymentOrderResponse { Success = false, Error = $"Server error ({(int)response.StatusCode}): {body}" };
                }
                catch
                {
                    return new CreatePaymentOrderResponse { Success = false, Error = $"Server error ({(int)response.StatusCode}): {body}" };
                }
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<CreatePaymentOrderResponse>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new CreatePaymentOrderResponse { Success = false, Error = "Empty response from server." };
        }
        catch (Exception ex)
        {
            return new CreatePaymentOrderResponse { Success = false, Error = $"Network error: {ex.Message}" };
        }
    }

    public async Task<CreatePaymentOrderResponse?> CreateOrderAnonymousAsync(AnonymousCreateOrderRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/payment/create-order-anonymous", request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Try to parse error from response body
                try
                {
                    var errorResult = System.Text.Json.JsonSerializer.Deserialize<CreatePaymentOrderResponse>(body,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return errorResult ?? new CreatePaymentOrderResponse { Success = false, Error = $"Server error ({(int)response.StatusCode}): {body}" };
                }
                catch
                {
                    return new CreatePaymentOrderResponse { Success = false, Error = $"Server error ({(int)response.StatusCode}): {body}" };
                }
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<CreatePaymentOrderResponse>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new CreatePaymentOrderResponse { Success = false, Error = "Empty response from server." };
        }
        catch (Exception ex)
        {
            return new CreatePaymentOrderResponse { Success = false, Error = $"Network error: {ex.Message}" };
        }
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
