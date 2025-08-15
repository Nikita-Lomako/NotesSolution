using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface IAuthRepository
    {
        Task<IdentityUser?> FindByNameAsync(string username, CancellationToken cancellationToken = default);
        Task<IdentityUser?> Login(string username, string password, CancellationToken cancellationToken = default);
        Task<IdentityUser?> Register(string username, string password, CancellationToken cancellationToken = default);
    }
}
