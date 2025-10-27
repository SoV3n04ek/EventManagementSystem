using EventManagement.Application.DTOs.UserDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Mapping;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces.Security;
using EventManagement.Infrastructure.Interfaces;

namespace EventManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService
        ) {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<UserDetailDto> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException($"User with id {userId} not found");

            return user.ToUserDetailDto();
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
           
            if (user == null || !_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new BadRequestException("Invalid email or password");
            }

            var token = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                User = user.ToUserDto()
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            if (await _userRepository.GetByEmailAsync(registerDto.Email) != null)
            {
                throw new ConflictException("User with this email already existed");
            }

            var user = new User
            {
                Name = registerDto.Name,
                Email = registerDto.Email.ToLower(),
                PasswordHash = _passwordHasher.HashPassword(registerDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user.ToUserDto();
        }
    }
}
