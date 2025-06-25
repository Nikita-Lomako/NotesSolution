using Microsoft.EntityFrameworkCore;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Infrastructure.Data;

namespace NotesSolution.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _db;

        public TagRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ICollection<Tag>> GetAllAsync()
        {
            return await _db.Tags.AsNoTracking().ToListAsync();
        }

        public async Task<Tag?> GetAsync(Guid id)
        {
            return await _db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await _db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task CreateAsync(Tag tag)
        {
            await _db.Tags.AddAsync(tag);
        }

        public async Task UpdateAsync(Tag tag)
        {
            _db.Tags.Update(tag);
        }

        public async Task RemoveAsync(Tag tag)
        {
            _db.Tags.Remove(tag);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
} 