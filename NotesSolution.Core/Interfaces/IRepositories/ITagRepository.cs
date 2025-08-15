using System.Threading;
using NotesSolution.Core.Models;
using System.Threading;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface ITagRepository
    {
        Task<ICollection<Tag>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
        Task<Tag?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default);
        Task<Tag?> GetByNameAsync(string userId, string name, CancellationToken cancellationToken = default);
        Task CreateAsync(Tag tag, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
        Task RemoveAsync(Tag tag, CancellationToken cancellationToken = default);
    }
}
