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

        var response = await base.SendAsync(request, cancellationToken);

        // If 401 Unauthorized, trigger session expired event ONLY if user was authenticated
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Don't trigger for login/register/refresh endpoints
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (!path.Contains("/auth/login") && !path.Contains("/auth/register") && !path.Contains("/auth/refresh"))
            {
                try
                {
                    // Only show popup if there was a token in localStorage (meaning user WAS logged in)
                    var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                    if (!string.IsNullOrEmpty(token))
                    {
                        await _jsRuntime.InvokeVoidAsync("SessionExpiredHandler.trigger");
                    }
                }
                catch { }
            }
        }

        return response;
    }
}
