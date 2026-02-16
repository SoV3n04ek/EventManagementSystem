using System.Net;
using System.Text.RegularExpressions;

namespace EventManagement.IntegrationTests;

/// <summary>
/// A DelegatingHandler that manages cookies via a CookieContainer.
/// 
/// The TestServer runs over HTTP, but the auth_token cookie has Secure flag.
/// Standard CookieContainer would reject Secure cookies on HTTP URIs.
/// This handler manually parses Set-Cookie headers and stores cookies
/// without enforcing the Secure constraint, since the TestServer
/// is a trusted in-process transport (no real network involved).
/// </summary>
public class CookieContainerHandler : DelegatingHandler
{
    // Simple cookie store: name → value
    private readonly Dictionary<string, string> _cookies = new();

    public CookieContainerHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Attach stored cookies to the outgoing request
        if (_cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", _cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", cookieHeader);
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Extract Set-Cookie headers and store them
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                ParseAndStoreCookie(header);
            }
        }

        return response;
    }

    private void ParseAndStoreCookie(string setCookieHeader)
    {
        // Set-Cookie format: name=value; path=/; httponly; secure; samesite=strict; expires=...
        // We extract just the name=value part (everything before the first ';')
        var parts = setCookieHeader.Split(';', 2);
        var nameValue = parts[0].Trim();

        var equalsIndex = nameValue.IndexOf('=');
        if (equalsIndex <= 0) return;

        var name = nameValue[..equalsIndex];
        var value = nameValue[(equalsIndex + 1)..];

        // Check if the cookie is being expired/deleted
        // (empty value or expires in the past)
        var isExpired = setCookieHeader.Contains("expires=", StringComparison.OrdinalIgnoreCase)
            && TryParseExpiry(setCookieHeader, out var expiryDate)
            && expiryDate < DateTimeOffset.UtcNow;

        if (isExpired || string.IsNullOrEmpty(value))
        {
            _cookies.Remove(name);
        }
        else
        {
            _cookies[name] = value;
        }
    }

    private static bool TryParseExpiry(string header, out DateTimeOffset expiry)
    {
        expiry = default;
        var match = Regex.Match(header, @"expires=([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        return DateTimeOffset.TryParse(match.Groups[1].Value, out expiry);
    }
}
