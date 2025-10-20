namespace EventManagement.Domain.Interfaces.Security
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Email { get; }
        string? Name { get; }
        bool IsAuthenticated { get; }
    }
}