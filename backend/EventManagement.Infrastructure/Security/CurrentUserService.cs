
using System.Security.Claims;
using EventManagement.Domain.Interfaces.Security;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Infrastructure.Security;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public int? UserId => GetUserId();
    public string? Email => GetClaimValue(ClaimTypes.Email);
    public string? Name => GetClaimValue(ClaimTypes.Name);
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?
        .User.Identity?.IsAuthenticated == true;

    int? GetUserId()
    {
        string? userId = GetClaimValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out int id) ? id : null;
    }

    string? GetClaimValue(string claimType) => httpContextAccessor.HttpContext?
            .User?
            .FindFirst(claimType)?
            .Value;
}
