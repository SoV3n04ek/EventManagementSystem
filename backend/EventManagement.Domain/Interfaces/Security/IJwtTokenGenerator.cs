using EventManagement.Domain.Entities;

namespace EventManagement.Domain.Interfaces.Security
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        int? ValidateToken(string token);
    }
}
