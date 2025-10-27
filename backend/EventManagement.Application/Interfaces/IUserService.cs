using EventManagement.Application.DTOs.UserDtos;

namespace EventManagement.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<UserDetailDto> GetUserByIdAsync(int userId);
}