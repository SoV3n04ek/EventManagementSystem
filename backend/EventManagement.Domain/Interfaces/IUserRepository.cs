using EventManagement.Domain.Entities;

namespace EventManagement.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task SaveChangesAsync();
        Task<User?> GetByIdAsync(int id);
    }
}
