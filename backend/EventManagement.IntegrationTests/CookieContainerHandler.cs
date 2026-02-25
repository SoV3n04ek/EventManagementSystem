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
public partial class CookieContainerHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    // Simple cookie store: name → value
    readonly Dictionary<string, string> cookies = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Attach stored cookies to the outgoing request
        if (cookies.Count > 0)
        {
            string cookieHeader = string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _ = request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", cookieHeader);
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Extract Set-Cookie headers and store them
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (string header in setCookies)
            {
                ParseAndStoreCookie(header);
            }
        }

        return response;
    }

    void ParseAndStoreCookie(string setCookieHeader)
    {
        // Set-Cookie format: name=value; path=/; httponly; secure; samesite=strict; expires=...
        // We extract just the name=value part (everything before the first ';')
        string[] parts = setCookieHeader.Split(';', 2);
        string nameValue = parts[0].Trim();

        int equalsIndex = nameValue.IndexOf('=');
        if (equalsIndex <= 0) return;

        string name = nameValue[..equalsIndex];
        string value = nameValue[(equalsIndex + 1)..];

        // Check if the cookie is being expired/deleted
        // (empty value or expires in the past)
        bool isExpired = setCookieHeader.Contains("expires=", StringComparison.OrdinalIgnoreCase)
            && TryParseExpiry(setCookieHeader, out var expiryDate)
            && expiryDate < DateTimeOffset.UtcNow;

        if (isExpired || string.IsNullOrEmpty(value))
        {
            _ = cookies.Remove(name);
        }
        else
        {
            cookies[name] = value;
        }
    }

    static bool TryParseExpiry(string header, out DateTimeOffset expiry)
    {
        expiry = default;
        var match = MyRegex().Match(header);
        return match.Success && DateTimeOffset.TryParse(match.Groups[1].Value, out expiry);
    }

    [GeneratedRegex(@"expires=([^;]+)", RegexOptions.IgnoreCase, "")]
    private static partial Regex MyRegex();
}
