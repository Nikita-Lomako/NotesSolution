using NotesSolution.Core.Models;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface INoteRepository
    {
        Task<ICollection<Note>> GetAllAsync(string? search, string? tag, string? sort, string? order, int page, int pageSize);
        Task<Note?> GetAsync(Guid id);
        Task CreateAsync(Note note);
        Task UpdateAsync(Note note);
        Task RemoveAsync(Note note);
        Task SaveAsync();
        Task<bool> ExistsAsync(Guid id);
    }
} 