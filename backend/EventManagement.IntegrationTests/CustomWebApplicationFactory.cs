using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManagement.Application.DTOs.UserDtos;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// 
/// Strategy:
/// ─────────────────────────────────────────────────
/// - REAL: Entire middleware pipeline (Auth, XSRF, Filters, Error Handling)
/// - MOCKED: IUserService, IEventService (no database dependency)
/// - IN-MEMORY: JWT configuration so tokens can be generated and validated
/// ─────────────────────────────────────────────────
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // ── Test JWT Configuration ──
    // Must match exactly between token generation and validation.
    public const string TestJwtKey = "ThisIsAVeryLongTestSecretKeyForJwtSigning123!";
    public const string TestJwtIssuer = "TestIssuer";
    public const string TestJwtAudience = "TestAudience";

    public Mock<IUserService> MockUserService { get; } = new();
    public Mock<IEventService> MockEventService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment FIRST — Program.cs uses this to skip migrations/seeding
        _ = builder.UseEnvironment("Testing");

        // Override configuration with in-memory JWT settings
        _ = builder.ConfigureAppConfiguration((context, config) => _ = config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestJwtKey,
            ["Jwt:Issuer"] = TestJwtIssuer,
            ["Jwt:Audience"] = TestJwtAudience,
            ["Jwt:ExpireHours"] = "24",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_db;Username=test;Password=test"
        }));


        _ = builder.ConfigureServices(services =>
        {
            // Remove real PostgreSQL DbContext registrations
            // Then replace with InMemory provider so repositories still resolve
            var dbDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EventManagementDbContext") == true
                         || d.ServiceType.FullName?.Contains("DbContextOptions") == true
                         || d.ServiceType.FullName?.Contains("Npgsql") == true)
                .ToList();
            foreach (var descriptor in dbDescriptors)
                _ = services.Remove(descriptor);

            // Re-register DbContext with InMemory provider
            _ = services.AddDbContext<Infrastructure.EventManagementDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // Remove health checks that depend on PostgreSQL
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var descriptor in healthCheckDescriptors)
                _ = services.Remove(descriptor);
            // Re-add basic health checks without DB dependency
            _ = services.AddHealthChecks();

            // Replace real services with mocks
            ReplaceService(services, MockUserService.Object);
            ReplaceService(services, MockEventService.Object);

            // Remove database seeder to prevent seed attempts
            var seederDescriptors = services
                .Where(d => d.ServiceType.Name.Contains("IDatabaseSeeder")
                         || d.ServiceType.Name.Contains("DatabaseSeeder"))
                .ToList();
            foreach (var descriptor in seederDescriptors)
                _ = services.Remove(descriptor);

            // ── Antiforgery Override for Testing ──
            // Program.cs sets SecurePolicy to Always, which fails in HTTP-based TestServer.
            _ = services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None);
        });
    }

    static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            _ = services.Remove(descriptor);
        _ = services.AddScoped(_ => implementation);
    }

    // ── Helper: Create an HttpClient with browser-like cookie management ──
    // The CookieContainerHandler stores cookies from Set-Cookie headers
    // and sends them back, simulating browser behavior.
    // It bypasses the Secure cookie constraint since TestServer uses HTTP.
    public HttpClient CreateCookieClient()
    {
        var serverHandler = Server.CreateHandler();
        var cookieHandler = new CookieContainerHandler(serverHandler);

        var client = new HttpClient(cookieHandler)
        {
            BaseAddress = Server.BaseAddress
        };
        return client;
    }

    // ── Helper: Generate a valid JWT for test users ──
    // Uses the same key/issuer/audience as the test configuration
    // so the auth middleware will accept these tokens.
    public static string GenerateTestJwt(int userId = 1, string name = "Test User", string email = "test@test.com")
    {
        byte[] key = Encoding.UTF8.GetBytes(TestJwtKey);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email)
            ]),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = TestJwtIssuer,
            Audience = TestJwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Configure the MockUserService to accept a login and return a valid JWT.
    /// </summary>
    public void SetupLoginSuccess(string email = "test@test.com", string password = "Password123!")
    {
        string jwt = GenerateTestJwt(email: email);

        _ = MockUserService
            .Setup(s => s.LoginAsync(It.Is<LoginDto>(dto =>
                dto.Email == email && dto.Password == password)))
            .ReturnsAsync(new AuthResponseDto
            {
                Token = jwt,
                User = new UserDto
                {
                    Id = 1,
                    Name = "Test User",
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                }
            });
    }
}
