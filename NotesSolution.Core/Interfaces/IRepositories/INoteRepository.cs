using NotesSolution.Core.Models;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface INoteRepository
    {
        Task<ICollection<Note>> GetAllAsync(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize);
        Task<Note?> GetAsync(string userId, Guid id);
        Task CreateAsync(Note note);
        Task UpdateAsync(Note note);
        Task RemoveAsync(Note note);
        Task SaveAsync();
        Task<bool> ExistsAsync(string userId, Guid id);
    }
} 