using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IExchangeApiService
{
    Task<ExchangeData?> GetExchangeDataAsync(string exchange);
}

public class ExchangeApiService : IExchangeApiService
{
    private readonly HttpClient _http;

    public ExchangeApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ExchangeData?> GetExchangeDataAsync(string exchange)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<ExchangeData>>($"exchange/{exchange}");
        return response?.Data;
    }
}
