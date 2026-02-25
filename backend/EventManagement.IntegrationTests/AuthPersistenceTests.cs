using System.Net;
using EventManagement.Application.DTOs.UserDtos;
using Moq;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Integration tests for HttpOnly cookie authentication.
/// Verifies:
///   1. Login sets auth_token as HttpOnly cookie with proper flags
///   2. Cookie-based auth grants access to protected endpoints
///   3. Logout properly expires all session cookies
/// </summary>
public class AuthPersistenceTests(CustomWebApplicationFactory factory) : TestBase(factory)
{
    [Fact]
    public async Task LoginSetsHttpOnlyAuthCookieWithCorrectFlags()
    {
        // Arrange
        Factory.SetupLoginSuccess();
        var client = Factory.CreateCookieClient();

        // Act
        var response = await LoginAsync(client);

        // Assert — Debug on failure
        string debug = await GetResponseBodyForDebug(response);
        Assert.True(response.IsSuccessStatusCode, $"Login failed: {debug}");

        // Verify the Set-Cookie header for auth_token
        string? authCookie = ExtractSetCookieHeader(response, "auth_token");
        Assert.NotNull(authCookie);

        // Verify cookie flags
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizedEndpointSucceedsWithCookie()
    {
        // Arrange
        Factory.SetupLoginSuccess();

        // Setup the /auth/me mock
        _ = Factory.MockUserService
            .Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new UserDetailDto
            {
                Id = 1,
                Name = "Test User",
                Email = "test@test.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        var client = Factory.CreateCookieClient();

        // Act — Login first (cookies stored in container automatically)
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // Act — Access protected endpoint (auth_token cookie sent automatically)
        var meResponse = await client.GetAsync("/api/auth/me");

        // Assert
        string debug = await GetResponseBodyForDebug(meResponse);
        Assert.True(meResponse.IsSuccessStatusCode,
            $"/auth/me failed after login: {debug}");
    }

    [Fact]
    public async Task LogoutExpiresAuthAndXsrfCookies()
    {
        // Arrange
        Factory.SetupLoginSuccess();
        var client = Factory.CreateCookieClient();

        // Login first
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // Get XSRF token for the logout POST
        string? xsrfToken = ExtractXsrfToken(loginResponse);
        Assert.NotNull(xsrfToken);

        // Act — Logout
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken);
        var logoutResponse = await client.SendAsync(logoutRequest);

        // Assert
        string debug = await GetResponseBodyForDebug(logoutResponse);
        Assert.True(logoutResponse.IsSuccessStatusCode,
            $"Logout failed: {debug}");

        // Verify auth_token is expired (Set-Cookie with past date)
        string? authCookieHeader = ExtractSetCookieHeader(logoutResponse, "auth_token");
        Assert.NotNull(authCookieHeader);
        Assert.Contains("expires=", authCookieHeader, StringComparison.OrdinalIgnoreCase);

        // After logout, accessing protected endpoint should fail
        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }
}
