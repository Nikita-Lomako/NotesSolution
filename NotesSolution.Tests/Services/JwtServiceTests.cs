using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using NotesSolution.Application.Services;
using Xunit;
using System.Linq;

namespace NotesSolution.Tests.Services
{
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;

        public JwtServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                { "ApiSettings:Secret", "supersecretkeysupersecretkeysupersecretkey" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            _jwtService = new JwtService(_configuration);
        }


        [Fact]
        public void GenerateToken_ReturnsJwt_WhenNoClaimsProvided()
        {
            var token = _jwtService.GenerateToken(new List<Claim>());
            Assert.False(string.IsNullOrWhiteSpace(token));
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var userClaims = jwt.Claims.Where(c => c.Type != "nbf" && c.Type != "exp" && c.Type != "iat");
            Assert.Empty(userClaims);
        }
    }
} 