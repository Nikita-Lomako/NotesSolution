using NotesSolution.Core.Models;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface ITagRepository
    {
        Task<ICollection<Tag>> GetAllAsync(string userId);
        Task<Tag?> GetAsync(string userId, Guid id);
        Task<Tag?> GetByNameAsync(string userId, string name);
        Task CreateAsync(Tag tag);
        Task SaveAsync();
        Task UpdateAsync(Tag tag);
        Task RemoveAsync(Tag tag);
    }
}