using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotesSolution.Infrastructure.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly AppDbContext _db;

        public NoteRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ICollection<Note>> GetAllAsync(string? search, string? tag, string? sort, string? order, int page, int pageSize)
        {
            var query = _db.Notes.Include(n => n.Tags).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(n => n.Name.ToLower().Contains(search.ToLower()) || n.Description.ToLower().Contains(search.ToLower()));
            }

            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(n => n.Tags.Any(t => t.Name.ToLower() == tag.ToLower()));
            }

            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort.ToLower())
                {
                    case "date":
                        query = order?.ToLower() == "desc" 
                            ? query.OrderByDescending(n => n.CreationDate) 
                            : query.OrderBy(n => n.CreationDate);
                        break;
                    case "name":
                        query = order?.ToLower() == "desc" 
                            ? query.OrderByDescending(n => n.Name) 
                            : query.OrderBy(n => n.Name);
                        break;
                }
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Note?> GetAsync(Guid id) => await _db.Notes.Include(n => n.Tags).FirstOrDefaultAsync(n => n.Id == id);
        public async Task CreateAsync(Note note)
        {
            for (int i = 0; i < note.Tags.Count; i++)
            {
                var tag = note.Tags[i];
                if (_db.Entry(tag).State == EntityState.Detached)
                {
                    var trackedTag = await _db.Tags.FindAsync(tag.Id);
                    if (trackedTag != null)
                    {
                        note.Tags[i] = trackedTag;
                    }
                    else
                    {
                        _db.Tags.Attach(tag);
                    }
                }
            }
            await _db.Notes.AddAsync(note);
            await SaveAsync();
        }
        public async Task UpdateAsync(Note note)
        {
            for (int i = 0; i < note.Tags.Count; i++)
            {
                var tag = note.Tags[i];
                if (_db.Entry(tag).State == EntityState.Detached)
                {
                    var trackedTag = await _db.Tags.FindAsync(tag.Id);
                    if (trackedTag != null)
                    {
                        note.Tags[i] = trackedTag;
                    }
                    else
                    {
                        _db.Tags.Attach(tag);
                    }
                }
            }
            _db.Notes.Update(note);
            await SaveAsync();
        }
        public async Task RemoveAsync(Note note)
        {
            var tracked = await _db.Notes.FindAsync(note.Id);
            if (tracked != null)
            {
                _db.Notes.Remove(tracked);
                await SaveAsync();
            }
        }
        public async Task SaveAsync() => await _db.SaveChangesAsync();
        public async Task<bool> ExistsAsync(Guid id) => await _db.Notes.AnyAsync(n => n.Id == id);
    }
} 