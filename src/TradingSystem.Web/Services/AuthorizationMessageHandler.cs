using Microsoft.JSInterop;

namespace TradingSystem.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public AuthorizationMessageHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // JSInterop might not be available during pre-rendering
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
