using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;
using NotesSolution.Application.Services;
using NotesSolution.Core.Interfaces.IRepositories;
using Xunit;

namespace NotesSolution.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IAuthRepository> _authRepositoryMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _authRepositoryMock = new Mock<IAuthRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _configurationMock = new Mock<IConfiguration>();
            _authService = new AuthService(_authRepositoryMock.Object, _configurationMock.Object, _jwtServiceMock.Object);
        }

        [Fact]
        public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
        {
            // Arrange
            var user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = "test", Id = "1" };
            _authRepositoryMock.Setup(r => r.Login("test", "pass")).ReturnsAsync(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<IEnumerable<Claim>>())).Returns("token");

            _authRepositoryMock.Setup(r => r.Login("test", "pass", It.IsAny<CancellationToken>())).ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("token", result.Token);
            Assert.Equal("test", result.UserName);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenCredentialsAreInvalid()
        {
            // Arrange
            _authRepositoryMock.Setup(r => r.Login("test", "wrong")).ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityUser?)null);
            var loginDto = new LoginRequestDto { UserName = "test", Password = "wrong" };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsUserDto_WhenRegistrationIsSuccessful()
        {
            // Arrange
            _authRepositoryMock.Setup(r => r.FindByNameAsync("newuser")).ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityUser?)null);
            var user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = "newuser", Id = "2" };
            _authRepositoryMock.Setup(r => r.Register("newuser", "pass")).ReturnsAsync(user);
            var regDto = new RegistrationRequestDto { UserName = "newuser", Password = "pass" };

            // Act
            var result = await _authService.RegisterAsync(regDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Id);
            Assert.Equal("newuser", result.Name);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsNull_WhenUserAlreadyExists()
        {
            // Arrange
            var user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = "existing", Id = "3" };
            _authRepositoryMock.Setup(r => r.FindByNameAsync("existing")).ReturnsAsync(user);
            var regDto = new RegistrationRequestDto { UserName = "existing", Password = "pass" };

            // Act
            var result = await _authService.RegisterAsync(regDto);

            // Assert
            Assert.Null(result);
        }
    }
} 