using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<ICancellationTokenProvider> _cancellationTokenProviderMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _authRepositoryMock = new Mock<IAuthRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _configurationMock = new Mock<IConfiguration>();
            _cancellationTokenProviderMock = new Mock<ICancellationTokenProvider>();
            var logger = Mock.Of<ILogger<AuthService>>();

            // Setup cancellation token provider
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);

            _authService = new AuthService(_authRepositoryMock.Object, _configurationMock.Object, _jwtServiceMock.Object, logger, _cancellationTokenProviderMock.Object);
        }

        [Fact]
        public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
        {
            // Arrange
            var user = new IdentityUser { UserName = "test", Id = "1" };
            var loginDto = new LoginRequestDto { UserName = "test", Password = "pass" };

            _authRepositoryMock.Setup(r => r.Login(loginDto.UserName, loginDto.Password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<IEnumerable<Claim>>()))
                .Returns("token");

            // Act
            var result = await _authService.LoginAsync(loginDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("token", result.Token);
            Assert.Equal("test", result.UserName);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = new LoginRequestDto { UserName = "test", Password = "wrong" };
            _authRepositoryMock.Setup(r => r.Login(loginDto.UserName, loginDto.Password, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _authService.LoginAsync(loginDto, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsUserDto_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var regDto = new RegistrationRequestDto { UserName = "newuser", Password = "pass" };
            var user = new IdentityUser { UserName = "newuser", Id = "2" };

            _authRepositoryMock.Setup(r => r.FindByNameAsync(regDto.UserName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IdentityUser?)null);
            _authRepositoryMock.Setup(r => r.Register(regDto.UserName, regDto.Password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.RegisterAsync(regDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Id);
            Assert.Equal("newuser", result.Name);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsNull_WhenUserAlreadyExists()
        {
            // Arrange
            var regDto = new RegistrationRequestDto { UserName = "existing", Password = "pass" };
            var user = new IdentityUser { UserName = "existing", Id = "3" };
            _authRepositoryMock.Setup(r => r.FindByNameAsync(regDto.UserName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.RegisterAsync(regDto, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
