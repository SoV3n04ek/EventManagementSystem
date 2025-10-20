
using EventManagement.Domain.Interfaces.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventManagement.Infrastructure.Security
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId => GetUserId();
        public string? Email => GetClaimValue(ClaimTypes.Email);
        public string? Name => GetClaimValue(ClaimTypes.Name);
        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?
            .User.Identity?.IsAuthenticated == true;

        private int? GetUserId()
        {
            var userId = GetClaimValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var id) ? id : null;
        }

        private string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(claimType)?
                .Value;
        }
    }
}
