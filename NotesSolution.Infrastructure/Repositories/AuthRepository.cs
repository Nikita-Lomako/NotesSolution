using Microsoft.AspNetCore.Identity;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Infrastructure.Data;
using System.Threading.Tasks;

namespace NotesSolution.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AuthRepository(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IdentityUser?> FindByNameAsync(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<IdentityUser?> Login(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
                return null;
            return user;
        }

        public async Task<IdentityUser?> Register(string username, string password)
        {
            if (await _userManager.FindByNameAsync(username) != null)
                return null;
            var user = new IdentityUser { UserName = username };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return null;
            return user;
        }
    }
} 