using EventManagement.Api.Middleware;
using EventManagement.Api.Filters;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Interfaces.Security;
using EventManagement.Infrastructure;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Interfaces;
using EventManagement.Infrastructure.Repository;
using EventManagement.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EventManagementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Minimal services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddCors();

var app = builder.Build();

// Force the Port (Railway Fix)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Run migrations but catch errors so they don't kill the server
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var context = scope.ServiceProvider.GetRequiredService<EventManagementDbContext>();
        // Migrate if not in testing
        if (!app.Environment.IsEnvironment("Testing"))
        {
            await context.Database.MigrateAsync();
            Console.WriteLine("Migrations completed successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

// The absolute first thing the app should do
app.MapGet("/", () => "API is running - Debug Mode");
app.MapGet("/health", () => new { status = "Healthy", time = DateTime.UtcNow });

// Essential Middleware
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapControllers();

// Run immed    iately
app.Run();

// var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddControllers(options =>
// {
//     // Global XSRF validation on all state-changing requests
//     options.Filters.Add<ValidateAntiforgeryFilter>();
// });

// // Health Checks
// builder.Services.AddHealthChecks()
//     .AddNpgSql(
//         builder.Configuration.GetConnectionString("DefaultConnection")!,
//         name: "postgresql",
//         timeout: TimeSpan.FromSeconds(15),
//         tags: new[] { "db", "sql", "postgresql" }
//     );

// builder.Services.AddFluentValidationAutoValidation();
// builder.Services.AddFluentValidationClientsideAdapters();
// builder.Services.AddValidatorsFromAssemblyContaining<IUserService>();

// builder.Services.AddDbContext<EventManagementDbContext>(options =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//     if (string.IsNullOrWhiteSpace(connectionString))
//     {
//         throw new InvalidOperationException("CRITICAL: Connection string 'DefaultConnection' is null or empty! Check your Environment Variables.");
//     }

//     options.UseNpgsql(connectionString);
// });

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(options =>
// {
//     options.SwaggerDoc("v1", new OpenApiInfo
//     {
//         Title = "Event Management API",
//         Version = "v1",
//         Description = "Event Management System API"
//     });

//     // Jwt Authentication support in swagger
//     options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "JWT Authorization header using the Bearer scheme. Example: \\\"Authorization: Bearer {token}\\\"\"",
//         Name = "Authorization",
//         In = ParameterLocation.Header,
//         Type = SecuritySchemeType.Http,
//         Scheme = "bearer",
//         BearerFormat = "JWT"    
//     });

//     options.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//              new OpenApiSecurityScheme
//              {
//                  Reference = new OpenApiReference
//                  {
//                      Type = ReferenceType.SecurityScheme,
//                      Id = "Bearer"
//                  }
//              },
//              Array.Empty<string>()
//         }
//     });
// });

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = builder.Configuration["Jwt:Issuer"],
//             ValidAudience = builder.Configuration["Jwt:Audience"],
//             IssuerSigningKey = new SymmetricSecurityKey(
//                 Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
//         };

//         // Read JWT from HttpOnly cookie as fallback ──
//         // If the Authorization header is present (Swagger/Postman), use it.
//         // Otherwise, fall back to the "auth_token" HttpOnly cookie.
//         options.Events = new JwtBearerEvents
//         {
//             OnMessageReceived = context =>
//             {
//                 if (string.IsNullOrEmpty(context.Token)
//                     && context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
//                 {
//                     context.Token = cookieToken;
//                 }
//                 return Task.CompletedTask;
//             }
//         };
//     });

// // ── Antiforgery (CSRF/XSRF) Configuration ──
// // .NET manages TWO tokens internally:
// //   1. CookieToken  → stored in ".AspNetCore.Antiforgery" (HttpOnly, server-side only)
// //   2. RequestToken → we manually set it as "XSRF-TOKEN" cookie (non-HttpOnly) for Angular
// // Angular reads "XSRF-TOKEN" → sends value as "X-XSRF-TOKEN" header → .NET validates both.
// builder.Services.AddAntiforgery(options =>
// {
//     // Internal cookie for server-side validation (Angular never sees this)
//     options.Cookie.Name = ".AspNetCore.Antiforgery";
//     options.Cookie.HttpOnly = true;
//     options.Cookie.SameSite = SameSiteMode.Strict;
//     options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
//     options.Cookie.IsEssential = true;
//     // The header Angular sends (must match withXsrfConfiguration headerName)
//     options.HeaderName = "X-XSRF-TOKEN";
// });

// // CORS Configuration
// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//     {
//         // Azure will provide this via an environment variable named:
//         // CORS__AllowedOrigins__0
//         string[] allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>();

//         if (allowedOrigins == null || allowedOrigins.Length == 0)
//         {
//             // Fallback to Development if nothing is in JSON or Env Vars
//             policy.WithOrigins("https://localhost:4200")
//                 .AllowAnyMethod()
//                 .AllowAnyHeader()
//                 .AllowCredentials();
//         }
//         else
//         {
//             // Use the origins found in configuration
//             policy.WithOrigins(allowedOrigins)
//                 .AllowAnyMethod()
//                 .AllowAnyHeader()
//                 .AllowCredentials();
//         }
//     });
// });

// // Dependency Injection for auth
// builder.Services.AddHttpContextAccessor();
// builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
// builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// builder.Services.AddScoped<ITokenService, TokenService>();

// builder.Services.AddScoped<IEventRepository, EventRepository>();
// builder.Services.AddScoped<IEventService, EventService>();
// builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
// builder.Services.AddScoped<IUserRepository, UserRepository>();
// builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
// builder.Services.AddScoped<IUserService, UserService>();

// builder.Services.AddAuthorization();

// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     // Tells .NET to look for specific headers:
//     // X-Forwarded-For: The original user's IP address.
//     // X-Forwarded-Proto: Whether the user used HTTP or HTTPS.
//     options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
   
//     // Security reset: By default, .NET only trusts local proxies (127.0.0.1).
//     // In cloud environments (Azure/AWS), we don't always know the internal IP of the proxy.
//     // Clearing these tells .NET to trust the headers coming from the cloud load balancer.
//     options.KnownNetworks.Clear();
//     options.KnownProxies.Clear();
// });

// // for Antiforgery Keys in docker container
// builder.Services.AddDataProtection()
//     .PersistKeysToFileSystem(new DirectoryInfo("/tmp/dp-keys")); // Linux always allows writing to /tmp

// var app = builder.Build();


// app.UseForwardedHeaders();

// // Applying migrations (skip in Testing environment — no real DB)
// if (!app.Environment.IsEnvironment("Testing"))
// {
//     using (var scope = app.Services.CreateScope())
//     {
//         var services = scope.ServiceProvider;

//         try
//         {
//             var context = services.GetRequiredService<EventManagementDbContext>();
//             await context.Database.MigrateAsync();
//         }
//         catch (Exception ex)
//         {
//             var logger = services.GetRequiredService<ILogger<Program>>();
//             logger.LogError(ex, "An error occurred while migrating the database.");
//         }
//     }

//     if (app.Environment.IsDevelopment())
//     {
//         app.UseSwagger();
//         app.UseSwaggerUI();
//         // Seeding

//         using (var scope = app.Services.CreateScope())
//         {
//             var services = scope.ServiceProvider;

//             try
//             {
//                 var seeder = services.GetRequiredService<IDatabaseSeeder>();
//                 await seeder.SeedAsync();
//             }
//             catch (Exception ex)
//             {
//                 var logger = services.GetRequiredService<ILogger<Program>>();
//                 logger.LogError(ex, "An error occurred while seeding the database.");
//             }
//         }
//     }
//     /*
//     else {
//         // Use HSTS in production (tells browsers to ONLY use HTTPS)
//         app.UseHsts();
//     }*/
// }

// // ── Security Headers Middleware ──
// // Placed FIRST in the response pipeline so even error pages carry protection.
// // CSP prevents XSS by restricting where scripts/styles/connections can originate.
// // Combined with HttpOnly auth cookies + XSRF identity binding, this completes
// // the defense-in-depth perimeter:
// //   - HttpOnly cookie  → attacker can't READ the JWT via JS
// //   - CSP connect-src  → attacker can't EXFILTRATE the XSRF-TOKEN to external domains
// //   - XSRF validation  → attacker can't FORGE requests without the bound token

// // Commented Content Secuirity Block

// /*
// app.Use(async (context, next) =>
// {
//     // ── Content Security Policy ──
//     // Angular requires 'unsafe-inline' for component-scoped styles (injected <style> tags).
//     // A nonce-based alternative would need custom build tooling + server-rendered nonce per
//     // request — impractical for an SPA. This is the standard Angular CSP trade-off.
//     var csp = string.Join("; ",
//         "default-src 'self'",
//         app.Environment.IsDevelopment()
//             ? "script-src 'self' 'unsafe-eval'"      // Angular JIT / HMR needs eval in dev
//             : "script-src 'self'",                    // Strict in production
//         "style-src 'self' 'unsafe-inline'",           // Angular component styles
//         "img-src 'self' data: https:",                // Local + data URIs + secure external
//         app.Environment.IsDevelopment()
//             ? "connect-src 'self' http://localhost:5000 http://localhost:4200 ws://localhost:4200"
//             : "connect-src 'self' frontend-eventmanagement-production.up.railway.app",                   // Only same-origin in production
//         "frame-ancestors 'none'",                     // Clickjacking prevention
//         "base-uri 'self'",                            // Prevent <base> tag hijacking
//         "form-action 'self'"                          // Prevent form submission to external domains
//     );

//     context.Response.Headers["Content-Security-Policy"] = csp;

//     // ── Additional Security Headers ──
//     context.Response.Headers["X-Content-Type-Options"] = "nosniff";       // Prevent MIME-sniffing
//     context.Response.Headers["X-Frame-Options"] = "DENY";                 // Clickjacking (legacy fallback)
//     context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

//     await next();
// });
// */
// app.UseCors();

// app.UseMiddleware<ErrorHandlingMiddleware>();

// //app.UseHttpsRedirection();
// app.UseAuthentication();
// app.UseAuthorization();

// // ── XSRF Cookie Middleware (AFTER auth so tokens bind to the real identity) ──
// // Placed AFTER UseAuthentication/UseAuthorization so that
// // GetAndStoreTokens captures the authenticated ClaimsPrincipal.
// // If the user is anonymous, tokens bind to the anonymous identity.
// // After login, AuthController regenerates tokens bound to the new user.
// /*app.Use(async (context, next) =>
// {
//     try {
//         var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
//         var tokenSet = antiforgery.GetAndStoreTokens(context);
//         context.Response.Cookies.Append("XSRF-TOKEN", tokenSet.RequestToken!, new CookieOptions {
//             HttpOnly = false,
//             Secure = true, // Force secure in Railway
//             SameSite = SameSiteMode.Strict,
//             Path = "/",
//             IsEssential = true
//         });
//     } catch (Exception ex) {
//         var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//         logger.LogError(ex, "Antiforgery token generation failed.");
//     }

//     await next(context);
// });*/

// // Register Routes
// app.MapControllers();
// app.MapGet("/", () => "API is running");
// // Health Check endpoints
// app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
// {
//     ResponseWriter = async (context, report) =>
//     {
//         context.Response.ContentType = "application/json";
//         var result = System.Text.Json.JsonSerializer.Serialize(new
//         {
//             status = report.Status.ToString(),
//             timestamp = DateTime.UtcNow,
//             environment = app.Environment.EnvironmentName,
//             checks = report.Entries.Select(e => new
//             {
//                 name = e.Key,
//                 status = e.Value.Status.ToString(),
//                 duration = e.Value.Duration.TotalMilliseconds
//             })
//         });
//         await context.Response.WriteAsync(result);
//     }
// });

// app.Run();

// // ── Required for WebApplicationFactory<Program> in integration tests ──
// // Top-level statements generate an implicit Program class that is internal.
// // This partial class declaration makes it accessible to the test project.
// public partial class Program { }