using EventManagement.Application.DTOs.UserDtos;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagement.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IAntiforgery antiforgery,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        // ── Centralized Cookie Configuration ──

        private static readonly CookieOptions AuthCookieOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        };

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            _logger.LogInformation("Registering new user: {Email}", registerDto.Email);

            var user = await _userService.RegisterAsync(registerDto);

            return Ok(new
            {
                message = "User registered successfully",
                user = user
            });
        }

        /// <summary>
        /// Login user — sets HttpOnly auth cookie, returns user in JSON body.
        /// The JWT is NOT exposed in the JSON response (preventing XSS token theft).
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for user: {Email}", loginDto.Email);

            var authResponse = await _userService.LoginAsync(loginDto);

            // Set the JWT as an HttpOnly cookie — browser manages it automatically.
            // The token never reaches JavaScript, preventing XSS token theft.
            var expireHours = 24; // Matches JWT expiry from appsettings.json
            Response.Cookies.Append("auth_token", authResponse.Token, new CookieOptions
            {
                HttpOnly = AuthCookieOptions.HttpOnly,
                Secure = AuthCookieOptions.Secure,
                SameSite = AuthCookieOptions.SameSite,
                Path = AuthCookieOptions.Path,
                IsEssential = AuthCookieOptions.IsEssential,
                Expires = DateTimeOffset.UtcNow.AddHours(expireHours)
            });

            // ── XSRF Identity Binding 
            // The user is now authenticated. Regenerate XSRF tokens so they are
            // bound to the new ClaimsPrincipal instead of the anonymous identity.
            // Without this, the next POST request would fail with identity mismatch.
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,        // Angular reads this via document.cookie
                Secure = false,          // Allow over HTTP in development
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            _logger.LogInformation("Login successful for {Email} — XSRF token regenerated", loginDto.Email);

            // Return ONLY the user object — token is in the cookie, not the JSON body.
            return Ok(new
            {
                message = "Login successful",
                data = new
                {
                    user = authResponse.User
                }
            });
        }

        /// <summary>
        /// Logout user — expires the auth_token HttpOnly cookie.
        /// Called by the Angular frontend's AuthService.logout().
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            // Expire the cookie by setting its expiry to the past
            Response.Cookies.Append("auth_token", string.Empty, new CookieOptions
            {
                HttpOnly = AuthCookieOptions.HttpOnly,
                Secure = AuthCookieOptions.Secure,
                SameSite = AuthCookieOptions.SameSite,
                Path = AuthCookieOptions.Path,
                IsEssential = AuthCookieOptions.IsEssential,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            return Ok(user);
        }

        private int GetCurrentUserId()
        {
            string userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            
            if (string.IsNullOrEmpty(userIdClaim) 
                || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}
