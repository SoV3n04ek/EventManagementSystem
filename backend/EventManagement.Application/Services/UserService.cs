using EventManagement.Application.DTOs.UserDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Mapping;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces.Security;
using EventManagement.Infrastructure.Interfaces;

namespace EventManagement.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService
    ) : IUserService
{
    readonly IUserRepository userRepository = userRepository;
    readonly IPasswordHasher passwordHasher = passwordHasher;
    readonly ITokenService tokenService = tokenService;

    public async Task<UserDetailDto> GetUserByIdAsync(int userId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException($"User with id {userId} not found");

        return user.ToUserDetailDto();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await userRepository.GetByEmailAsync(loginDto.Email);

        if (user == null || !passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            throw new BadRequestException("Invalid email or password");
        }

        string token = tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = user.ToUserDto()
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await userRepository.GetByEmailAsync(registerDto.Email) != null)
        {
            throw new ConflictException("User with this email already existed");
        }

        var user = new User
        {
            Name = registerDto.Name,
            Email = registerDto.Email.ToLowerInvariant(),
            PasswordHash = passwordHasher.HashPassword(registerDto.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        return user.ToUserDto();
    }
}
