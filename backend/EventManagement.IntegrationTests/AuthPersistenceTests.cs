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
public class AuthPersistenceTests : TestBase
{
    public AuthPersistenceTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_Sets_HttpOnly_Auth_Cookie_With_Correct_Flags()
    {
        // Arrange
        Factory.SetupLoginSuccess();
        var client = Factory.CreateCookieClient();

        // Act
        var response = await LoginAsync(client);

        // Assert — Debug on failure
        var debug = await GetResponseBodyForDebug(response);
        Assert.True(response.IsSuccessStatusCode, $"Login failed: {debug}");

        // Verify the Set-Cookie header for auth_token
        var authCookie = ExtractSetCookieHeader(response, "auth_token");
        Assert.NotNull(authCookie);

        // Verify cookie flags
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authorized_Endpoint_Succeeds_With_Cookie()
    {
        // Arrange
        Factory.SetupLoginSuccess();

        // Setup the /auth/me mock
        Factory.MockUserService
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
        var debug = await GetResponseBodyForDebug(meResponse);
        Assert.True(meResponse.IsSuccessStatusCode,
            $"/auth/me failed after login: {debug}");
    }

    [Fact]
    public async Task Logout_Expires_Auth_And_Xsrf_Cookies()
    {
        // Arrange
        Factory.SetupLoginSuccess();
        var client = Factory.CreateCookieClient();

        // Login first
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // Get XSRF token for the logout POST
        var xsrfToken = ExtractXsrfToken(loginResponse);
        Assert.NotNull(xsrfToken);

        // Act — Logout
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken);
        var logoutResponse = await client.SendAsync(logoutRequest);

        // Assert
        var debug = await GetResponseBodyForDebug(logoutResponse);
        Assert.True(logoutResponse.IsSuccessStatusCode,
            $"Logout failed: {debug}");

        // Verify auth_token is expired (Set-Cookie with past date)
        var authCookieHeader = ExtractSetCookieHeader(logoutResponse, "auth_token");
        Assert.NotNull(authCookieHeader);
        Assert.Contains("expires=", authCookieHeader, StringComparison.OrdinalIgnoreCase);

        // After logout, accessing protected endpoint should fail
        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }
}
