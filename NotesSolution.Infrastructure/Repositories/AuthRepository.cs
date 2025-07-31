using Microsoft.AspNetCore.Identity;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NotesSolution.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(UserManager<IdentityUser> userManager, ILogger<AuthRepository> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IdentityUser?> FindByNameAsync(string username, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Finding user by name {Username}", username);
                
                var result = await _userManager.FindByNameAsync(username);
                
                if (result != null)
                {
                    _logger.LogDebug("Found user {Username}", username);
                }
                else
                {
                    _logger.LogDebug("User {Username} not found", username);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Find user by name operation was cancelled for {Username}", username);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding user {Username}", username);
                throw;
            }
        }

        public async Task<IdentityUser?> Login(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Login attempt for user {Username}", username);
                
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Login failed for user {Username} - user not found", username);
                    return null;
                }
                
                var passwordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!passwordValid)
                {
                    _logger.LogWarning("Login failed for user {Username} - invalid password", username);
                    return null;
                }
                
                _logger.LogDebug("Login successful for user {Username}", username);
                return user;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Login operation was cancelled for user {Username}", username);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", username);
                throw;
            }
        }

        public async Task<IdentityUser?> Register(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Registration attempt for user {Username}", username);
                
                var existingUser = await _userManager.FindByNameAsync(username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed for user {Username} - user already exists", username);
                    return null;
                }
                
                var user = new IdentityUser { UserName = username };
                var result = await _userManager.CreateAsync(user, password);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Registration failed for user {Username} - {Errors}", username, errors);
                    return null;
                }
                
                _logger.LogDebug("Registration successful for user {Username}", username);
                return user;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Registration operation was cancelled for user {Username}", username);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", username);
                throw;
            }
        }
    }
} 