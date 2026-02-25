using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventManagement.Api.Filters;

/// <summary>
/// Global action filter that validates the XSRF token on all
/// state-changing HTTP methods (POST, PUT, PATCH, DELETE).
/// 
/// Exemptions:
/// ─────────────────────────────────────────────────
/// 1. Safe methods: GET, HEAD, OPTIONS, TRACE
/// 2. [AllowAnonymous] endpoints: Login, Register
///    (CSRF attacks exploit authenticated sessions —
///     unauthenticated endpoints don't need CSRF protection
///     because there is no session cookie to hijack.)
/// 3. [IgnoreAntiforgeryToken] endpoints (explicit opt-out)
/// ─────────────────────────────────────────────────
/// 
/// Synchronized with Angular's withXsrfConfiguration():
/// - Angular reads the "XSRF-TOKEN" cookie (non-HttpOnly)
/// - Angular sends it back as the "X-XSRF-TOKEN" header
/// - This filter validates the header against the cookie
/// </summary>
public class ValidateAntiforgeryFilter(
    IAntiforgery antiforgery,
    ILogger<ValidateAntiforgeryFilter> logger) : IAsyncActionFilter
{
    static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    readonly IAntiforgery antiforgery = antiforgery;
    readonly ILogger<ValidateAntiforgeryFilter> logger = logger;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        string method = context.HttpContext.Request.Method;

        // ── Exemption 1: Safe (idempotent) HTTP methods ──
        if (SafeMethods.Contains(method))
        {
            _ = await next();
            return;
        }

        // ── Exemption 2: [AllowAnonymous] endpoints ──
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            logger.LogDebug("XSRF: Skipping validation for [AllowAnonymous] endpoint {Path}",
                context.HttpContext.Request.Path);
            _ = await next();
            return;
        }

        // ── Exemption 3: [IgnoreAntiforgeryToken] explicit opt-out ──
        if (endpoint?.Metadata.GetMetadata<IgnoreAntiforgeryTokenAttribute>() != null)
        {
            logger.LogDebug("XSRF: Skipping validation for [IgnoreAntiforgeryToken] endpoint {Path}",
                context.HttpContext.Request.Path);
            _ = await next();
            return;
        }

        // ── Validate XSRF token ──
        bool hasHeader = context.HttpContext.Request.Headers.ContainsKey("X-XSRF-TOKEN");
        bool hasCookie = context.HttpContext.Request.Cookies.ContainsKey(".AspNetCore.Antiforgery");

        logger.LogDebug(
            "XSRF: Validating {Method} {Path} — X-XSRF-TOKEN header present: {HasHeader}, " +
            ".AspNetCore.Antiforgery cookie present: {HasCookie}",
            method, context.HttpContext.Request.Path, hasHeader, hasCookie);

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            logger.LogDebug("XSRF: Validation succeeded for {Method} {Path}",
                method, context.HttpContext.Request.Path);
        }
        catch (AntiforgeryValidationException ex)
        {
            // ── Identity Binding Debug (Phase 1c) ──
            // Log the current identity so we can distinguish:
            //   - Identity mismatch (token bound to wrong user)
            //   - Missing header/cookie
            var identity = context.HttpContext.User.Identity;
            logger.LogWarning(
                "XSRF: Identity debug — IsAuthenticated: {IsAuth}, " +
                "AuthType: {AuthType}, Name: {Name}",
                identity?.IsAuthenticated,
                identity?.AuthenticationType,
                identity?.Name ?? "(anonymous)");

            logger.LogWarning(
                "XSRF: Validation FAILED for {Method} {Path}. " +
                "Header present: {HasHeader}, Cookie present: {HasCookie}. " +
                "Reason: {Reason}",
                method, context.HttpContext.Request.Path,
                hasHeader, hasCookie, ex.Message);

            context.Result = new BadRequestObjectResult(new
            {
                error = "XSRF Token Invalid",
                details = "The anti-forgery token could not be validated. " +
                          "Ensure the X-XSRF-TOKEN header is present and matches the XSRF-TOKEN cookie.",
                statusCode = StatusCodes.Status400BadRequest
            });
            return;
        }

        _ = await next();
    }
}
