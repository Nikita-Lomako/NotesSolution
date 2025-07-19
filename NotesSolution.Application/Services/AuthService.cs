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

        public AuthService(IAuthRepository authRepository, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
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
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["ApiSettings:Secret"] ?? throw new ArgumentNullException("Secret key is missing"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResponseDto
            {
                Token = tokenHandler.WriteToken(token),
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