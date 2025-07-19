using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface IAuthRepository
    {
        Task<IdentityUser?> FindByNameAsync(string username);
        Task<IdentityUser?> Login(string username, string password);
        Task<IdentityUser?> Register(string username, string password);
    }
} 