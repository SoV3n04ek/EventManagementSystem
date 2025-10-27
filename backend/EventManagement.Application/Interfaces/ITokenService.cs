using EventManagement.Domain.Entities;

namespace EventManagement.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
