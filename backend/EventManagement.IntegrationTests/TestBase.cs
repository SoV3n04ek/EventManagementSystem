using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Base class for integration tests providing cookie-aware HTTP clients
/// and XSRF token extraction utilities.
///
/// Key Concepts:
/// ─────────────────────────────────────────────────
/// CookieContainer: Simulates browser cookie persistence.
///   - After login, the auth_token cookie is stored automatically.
///   - Subsequent requests include it, just like a browser does.
///
/// XSRF Extraction: Parses the Set-Cookie header to get XSRF-TOKEN,
///   then manually sets X-XSRF-TOKEN request header (mimicking Angular).
/// ─────────────────────────────────────────────────
/// </summary>
public abstract class TestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;

    protected TestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    /// <summary>
    /// Extract the XSRF-TOKEN value from Set-Cookie response headers.
    /// This mimics what Angular does via document.cookie.
    /// </summary>
    protected static string? ExtractXsrfToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        foreach (var cookie in cookies)
        {
            // Match: XSRF-TOKEN=<value>;
            var match = Regex.Match(cookie, @"XSRF-TOKEN=([^;]+)");
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Extract a specific cookie's raw Set-Cookie header line.
    /// Used for inspecting cookie flags (HttpOnly, Secure, SameSite).
    /// </summary>
    protected static string? ExtractSetCookieHeader(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        return cookies.FirstOrDefault(c => c.StartsWith($"{cookieName}="));
    }

    /// <summary>
    /// Perform a full login flow:
    /// 1. GET initial page to receive anonymous XSRF tokens
    /// 2. POST /api/auth/login with credentials + XSRF header
    /// 3. Returns the login response (cookies are in the container)
    /// </summary>
    protected async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email = "test@test.com",
        string password = "Password123!")
    {
        // Step 1: Bootstrap — get initial XSRF tokens
        var bootstrapResponse = await client.GetAsync("/health");
        var initialXsrf = ExtractXsrfToken(bootstrapResponse);

        // Step 2: Login with XSRF header
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password })
        };

        if (initialXsrf != null)
        {
            loginRequest.Headers.Add("X-XSRF-TOKEN", initialXsrf);
        }

        return await client.SendAsync(loginRequest);
    }

    /// <summary>
    /// Log response body on failure for debugging.
    /// </summary>
    protected static async Task<string> GetResponseBodyForDebug(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return $"Status: {response.StatusCode} ({(int)response.StatusCode})\nBody: {body}";
    }
}
