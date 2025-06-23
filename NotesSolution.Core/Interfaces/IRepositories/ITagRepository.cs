using NotesSolution.Core.Models;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface ITagRepository
    {
        Task<ICollection<Tag>> GetAllAsync();
        Task<Tag?> GetAsync(Guid id);
        Task<Tag?> GetByNameAsync(string name);
        Task CreateAsync(Tag tag);
        Task SaveAsync();
        Task UpdateAsync(Tag tag);
        Task RemoveAsync(Tag tag);
    }
}