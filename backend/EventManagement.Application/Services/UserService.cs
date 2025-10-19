using EventManagement.Application.DTOs.UserDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public Task<UserDetailDto> GetUserByIdAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<string> LoginAsync(LoginDtoValidator loginDto)
        {
            throw new NotImplementedException();
        }

        public Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            throw new NotImplementedException();
        }

        //public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        //{
        //    if (await _userRepository.GetByEmailAsync(registerDto.Email) != null)
        //    {
        //        throw new ConflictException("User with this email already exists");
        //    }

        //    var passwordHash = HashPassword(registerDto.Password);

        //    var user = new User
        //    {
        //        Name = registerDto.Name,
        //        Email = registerDto.Email.ToLower()
        //    };
        //}

    }
}
