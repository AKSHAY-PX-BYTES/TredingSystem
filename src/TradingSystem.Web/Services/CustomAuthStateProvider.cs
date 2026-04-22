using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TradingSystem.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public CustomAuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

            if (string.IsNullOrEmpty(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var claims = ParseClaimsFromJwt(token);

            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out var expValue))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expValue).UtcDateTime;
                if (expDate < DateTime.UtcNow)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return claims;

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs == null) return claims;

            foreach (var kvp in keyValuePairs)
            {
                var claimType = kvp.Key switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    "email" => ClaimTypes.Email,
                    "given_name" => ClaimTypes.GivenName,
                    _ => kvp.Key
                };

                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                            claims.Add(new Claim(claimType, item.ToString()));
                    }
                    else
                    {
                        claims.Add(new Claim(claimType, element.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(claimType, kvp.Value?.ToString() ?? ""));
                }
            }
        }
        catch
        {
            // Invalid token format
        }
        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64.Replace('-', '+').Replace('_', '/'));
    }
}
