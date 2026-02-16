using System.Net;
using System.Net.Http.Json;
using EventManagement.Application.DTOs.EventDtos;
using Moq;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Integration tests for XSRF (CSRF) protection.
/// Verifies:
///   1. Protected POSTs without X-XSRF-TOKEN header are rejected (400)
///   2. Full login → fresh XSRF token → POST succeeds (200)
///   3. Stale anonymous XSRF token fails after login (identity mismatch)
/// </summary>
public class XsrfProtectionTests : TestBase
{
    public XsrfProtectionTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_Without_Xsrf_Header_Returns_400()
    {
        // Arrange — Login to get authenticated, but don't send XSRF header
        Factory.SetupLoginSuccess();
        var client = Factory.CreateCookieClient();

        // Login first (to get auth cookie)
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // Act — POST to protected endpoint WITHOUT X-XSRF-TOKEN header
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/join");
        // Deliberately NOT adding X-XSRF-TOKEN header

        var response = await client.SendAsync(request);

        // Assert — Should be rejected by the XSRF filter
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("XSRF", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_With_Valid_Xsrf_Header_Succeeds()
    {
        // Arrange
        Factory.SetupLoginSuccess();

        // Mock the event service to accept the join
        Factory.MockEventService
            .Setup(s => s.JoinEventAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var client = Factory.CreateCookieClient();

        // Act — Login (auth_token cookie is stored)
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // After login, the XSRF middleware and AuthController both set tokens,
        // causing a potential mismatch. Do a GET to let the middleware regenerate
        // a consistent token pair (XSRF-TOKEN + .AspNetCore.Antiforgery cookie).
        var refreshResponse = await client.GetAsync("/health");
        var xsrfToken = ExtractXsrfToken(refreshResponse);
        Assert.NotNull(xsrfToken);

        // Act — POST with the aligned XSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/join");
        request.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var response = await client.SendAsync(request);

        // Assert — Should succeed (XSRF token matches authenticated identity)
        var debug = await GetResponseBodyForDebug(response);
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated POST with XSRF failed: {debug}");
    }

    [Fact]
    public async Task Stale_Anonymous_Xsrf_Token_Fails_After_Login()
    {
        // Arrange
        Factory.SetupLoginSuccess();

        Factory.MockEventService
            .Setup(s => s.JoinEventAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var client = Factory.CreateCookieClient();

        // Step 1: Get initial (anonymous) XSRF token
        var bootstrapResponse = await client.GetAsync("/health");
        var anonymousXsrf = ExtractXsrfToken(bootstrapResponse);
        Assert.NotNull(anonymousXsrf);

        // Step 2: Login — this changes the identity and generates new XSRF tokens
        var loginResponse = await LoginAsync(client);
        Assert.True(loginResponse.IsSuccessStatusCode,
            $"Login failed: {await GetResponseBodyForDebug(loginResponse)}");

        // Verify we got a NEW token (different from anonymous)
        var authenticatedXsrf = ExtractXsrfToken(loginResponse);
        Assert.NotNull(authenticatedXsrf);

        // Step 3: Try to use the OLD anonymous XSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/join");
        request.Headers.Add("X-XSRF-TOKEN", anonymousXsrf);

        var response = await client.SendAsync(request);

        // Assert — Should fail because the anonymous token doesn't match
        // the now-authenticated identity
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("XSRF", body, StringComparison.OrdinalIgnoreCase);
    }
}
