using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repository;

public class UserRepository(EventManagementDbContext context) : IUserRepository
{
    readonly EventManagementDbContext context = context;

    public async Task<User?> GetByEmailAsync(string email) => await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public async Task<User?> GetByIdAsync(int id) => await context.Users
            .Include(u => u.OrganizedEvents)
            .Include(u => u.Participations)
            .ThenInclude(p => p.Event)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> ExistsByEmailAsync(string email) => await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public async Task AddAsync(User user) => _ = await context.Users.AddAsync(user);

    public async Task SaveChangesAsync() => _ = await context.SaveChangesAsync();
}
