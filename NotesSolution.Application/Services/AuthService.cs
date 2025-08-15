using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;

namespace NotesSolution.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            IJwtService jwtService,
            ILogger<AuthService> logger,
            ICancellationTokenProvider cancellationTokenProvider)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _jwtService = jwtService;
            _logger = logger;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutTokenSource.Token,
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Login attempt for user {UserName}", loginRequestDto.UserName);

                // Check cancellation before authentication
                combinedToken.ThrowIfCancellationRequested();

                var user = await _authRepository.Login(loginRequestDto.UserName, loginRequestDto.Password, combinedToken);
                if (user == null)
                {
                    _logger.LogWarning("Login failed for user {UserName} - invalid credentials", loginRequestDto.UserName);
                    return null;
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                // JWT Token
                var token = _jwtService.GenerateToken(claims);

                _logger.LogInformation("Login successful for user {UserName}", loginRequestDto.UserName);

                return new LoginResponseDto
                {
                    Token = token,
                    UserName = user.UserName ?? ""
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Login operation was cancelled for user {UserName}", loginRequestDto.UserName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {UserName}", loginRequestDto.UserName);
                throw;
            }
        }

        public async Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutTokenSource.Token,
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Registration attempt for user {UserName}", requestDto.UserName);

                // Check cancellation before checking existing user
                combinedToken.ThrowIfCancellationRequested();

                var existingUser = await _authRepository.FindByNameAsync(requestDto.UserName, combinedToken);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed for user {UserName} - user already exists", requestDto.UserName);
                    return null;
                }

                // Check cancellation before registration
                combinedToken.ThrowIfCancellationRequested();

                var user = await _authRepository.Register(requestDto.UserName, requestDto.Password, combinedToken);
                if (user == null)
                {
                    _logger.LogWarning("Registration failed for user {UserName} - registration process failed", requestDto.UserName);
                    return null;
                }

                _logger.LogInformation("Registration successful for user {UserName}", requestDto.UserName);

                return new UserDto { Id = user.Id, Name = user.UserName ?? "" };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Registration operation was cancelled for user {UserName}", requestDto.UserName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {UserName}", requestDto.UserName);
                throw;
            }
        }
    }
}
