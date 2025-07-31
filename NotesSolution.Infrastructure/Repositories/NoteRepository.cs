using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NotesSolution.Infrastructure.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NoteRepository> _logger;

        public NoteRepository(AppDbContext db, ILogger<NoteRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ICollection<Note>> GetAllAsync(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cancellation before starting database query
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Getting notes for user {UserId} with search={Search}, tag={Tag}, sort={Sort}, order={Order}, page={Page}, pageSize={PageSize}", 
                    userId, search, tag, sort, order, page, pageSize);

                var query = _db.Notes.Include(n => n.Tags).Where(n => n.UserId == userId).AsQueryable();

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

                var result = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} notes for user {UserId}", result.Count, userId);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get notes operation was cancelled for user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Note?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Getting note {Id} for user {UserId}", id, userId);
                
                var result = await _db.Notes.Include(n => n.Tags).FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogDebug("Found note {Id} for user {UserId}", id, userId);
                }
                else
                {
                    _logger.LogDebug("Note {Id} not found for user {UserId}", id, userId);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get note operation was cancelled for user {UserId}, note {Id}", userId, id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task CreateAsync(Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Creating note {Id} for user {UserId}", note.Id, note.UserId);

                for (int i = 0; i < note.Tags.Count; i++)
                {
                    var tag = note.Tags[i];
                    if (_db.Entry(tag).State == EntityState.Detached)
                    {
                        var trackedTag = await _db.Tags.FindAsync(new object[] { tag.Id }, cancellationToken);
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
                
                await _db.Notes.AddAsync(note, cancellationToken);
                await SaveAsync(cancellationToken);
                
                _logger.LogDebug("Successfully created note {Id} for user {UserId}", note.Id, note.UserId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Create note operation was cancelled for user {UserId}, note {Id}", note.UserId, note.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note {Id} for user {UserId}", note.Id, note.UserId);
                throw;
            }
        }

        public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Updating note {Id} for user {UserId}", note.Id, note.UserId);

                for (int i = 0; i < note.Tags.Count; i++)
                {
                    var tag = note.Tags[i];
                    if (_db.Entry(tag).State == EntityState.Detached)
                    {
                        var trackedTag = await _db.Tags.FindAsync(new object[] { tag.Id }, cancellationToken);
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
                await SaveAsync(cancellationToken);
                
                _logger.LogDebug("Successfully updated note {Id} for user {UserId}", note.Id, note.UserId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update note operation was cancelled for user {UserId}, note {Id}", note.UserId, note.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {Id} for user {UserId}", note.Id, note.UserId);
                throw;
            }
        }

        public async Task RemoveAsync(Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Removing note {Id} for user {UserId}", note.Id, note.UserId);
                
                var tracked = await _db.Notes.FindAsync(new object[] { note.Id }, cancellationToken);
                if (tracked != null)
                {
                    _db.Notes.Remove(tracked);
                    await SaveAsync(cancellationToken);
                    _logger.LogDebug("Successfully removed note {Id} for user {UserId}", note.Id, note.UserId);
                }
                else
                {
                    _logger.LogWarning("Note {Id} not found for removal for user {UserId}", note.Id, note.UserId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Remove note operation was cancelled for user {UserId}, note {Id}", note.UserId, note.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing note {Id} for user {UserId}", note.Id, note.UserId);
                throw;
            }
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Save changes operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await _db.Notes.AnyAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Exists check operation was cancelled for user {UserId}, note {Id}", userId, id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of note {Id} for user {UserId}", id, userId);
                throw;
            }
        }
    }
} 