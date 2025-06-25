using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NotesSolution.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly string _secretKey;

        public AuthRepository(UserManager<IdentityUser> userManager, IConfiguration configuration, IMapper mapper, AppDbContext db)
        {
            _userManager = userManager;
            _configuration = configuration;
            _mapper = mapper;
            _db = db;
            _secretKey = _configuration["ApiSettings:Secret"]
                ?? throw new ArgumentNullException("Secret key is missing");
        }

        public bool IsUniqueUser(string username)
        {
            var user = _db.Users.FirstOrDefault(x => x.UserName == username);
            return user == null;
        }

        public async Task<LoginResponseDto?> Login(LoginRequestDto loginRequestDto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(x => x.UserName == loginRequestDto.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequestDto.Password))
                return null;

            // Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = tokenHandler.WriteToken(token)
            };
        }

        public async Task<UserDto?> Register(RegistrationRequestDto requestDto)
        {
            var user = new IdentityUser { UserName = requestDto.UserName };
            if (await _userManager.FindByNameAsync(requestDto.UserName) != null)
                return null;

            var result = await _userManager.CreateAsync(user, requestDto.Password);
            if (!result.Succeeded)
                return null;

            return _mapper.Map<UserDto>(user);
        }
    }
} 