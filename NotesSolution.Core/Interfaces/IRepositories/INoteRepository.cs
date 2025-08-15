using System.Threading;
using NotesSolution.Core.Models;
using System.Threading;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface INoteRepository
    {
        Task<ICollection<Note>> GetAllAsync(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<Note?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default);
        Task CreateAsync(Note note, CancellationToken cancellationToken = default);
        Task UpdateAsync(Note note, CancellationToken cancellationToken = default);
        Task RemoveAsync(Note note, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    }
}
