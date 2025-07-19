using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NotesSolution.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IJwtService jwtService)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _jwtService = jwtService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
        {
            var user = await _authRepository.Login(loginRequestDto.UserName, loginRequestDto.Password);
            if (user == null)
                return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // JWT Token
            var token = _jwtService.GenerateToken(claims);

            return new LoginResponseDto
            {
                Token = token,
                UserName = user.UserName ?? ""
            };
        }

        public async Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto)
        {
            var existingUser = await _authRepository.FindByNameAsync(requestDto.UserName);
            if (existingUser != null)
                return null;

            var user = await _authRepository.Register(requestDto.UserName, requestDto.Password);
            if (user == null)
                return null;

            return new UserDto { Id = user.Id, Name = user.UserName ?? "" };
        }
    }
} 