using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Infrastructure.Data;

namespace NotesSolution.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TagRepository> _logger;

        public TagRepository(AppDbContext db, ILogger<TagRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ICollection<Tag>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Getting all tags for user {UserId}", userId);

                var result = await _db.Tags.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} tags for user {UserId}", result.Count, userId);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get tags operation was cancelled for user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Tag?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Getting tag {Id} for user {UserId}", id, userId);

                var result = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

                if (result != null)
                {
                    _logger.LogDebug("Found tag {Id} for user {UserId}", id, userId);
                }
                else
                {
                    _logger.LogDebug("Tag {Id} not found for user {UserId}", id, userId);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get tag operation was cancelled for user {UserId}, tag {Id}", userId, id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<Tag?> GetByNameAsync(string userId, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Getting tag by name {Name} for user {UserId}", name, userId);

                var result = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower() && t.UserId == userId, cancellationToken);

                if (result != null)
                {
                    _logger.LogDebug("Found tag {Name} for user {UserId}", name, userId);
                }
                else
                {
                    _logger.LogDebug("Tag {Name} not found for user {UserId}", name, userId);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get tag by name operation was cancelled for user {UserId}, tag {Name}", userId, name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag {Name} for user {UserId}", name, userId);
                throw;
            }
        }

        public async Task CreateAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Creating tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);

                await _db.Tags.AddAsync(tag, cancellationToken);
                await SaveAsync(cancellationToken);

                _logger.LogDebug("Successfully created tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Create tag operation was cancelled for user {UserId}, tag {Name}", tag.UserId, tag.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag {Name} for user {UserId}", tag.Name, tag.UserId);
                throw;
            }
        }

        public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Updating tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);

                _db.Tags.Update(tag);
                await SaveAsync(cancellationToken);

                _logger.LogDebug("Successfully updated tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update tag operation was cancelled for user {UserId}, tag {Id}", tag.UserId, tag.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {Id} for user {UserId}", tag.Id, tag.UserId);
                throw;
            }
        }

        public async Task RemoveAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Removing tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);

                var tracked = await _db.Tags.FindAsync(new object[] { tag.Id }, cancellationToken);
                if (tracked != null)
                {
                    _db.Tags.Remove(tracked);
                    await SaveAsync(cancellationToken);
                    _logger.LogDebug("Successfully removed tag {Id} with name {Name} for user {UserId}", tag.Id, tag.Name, tag.UserId);
                }
                else
                {
                    _logger.LogWarning("Tag {Id} not found for removal for user {UserId}", tag.Id, tag.UserId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Remove tag operation was cancelled for user {UserId}, tag {Id}", tag.UserId, tag.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {Id} for user {UserId}", tag.Id, tag.UserId);
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
    }
}
