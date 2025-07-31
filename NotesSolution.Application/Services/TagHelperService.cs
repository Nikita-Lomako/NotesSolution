using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NotesSolution.Application.Services
{
    public class TagHelperService : ITagHelperService
    {
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<TagHelperService> _logger;

        public TagHelperService(ITagRepository tagRepository, ILogger<TagHelperService> logger)
        {
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public async Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tagEntities = new List<Tag>();
                
                foreach (var tagName in tagNames)
                {
                    // Check cancellation before each tag operation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var existingTag = await _tagRepository.GetByNameAsync(userId, tagName, cancellationToken);
                    if (existingTag != null && existingTag.UserId == userId)
                    {
                        tagEntities.Add(existingTag);
                        _logger.LogDebug("Using existing tag {TagName} for user {UserId}", tagName, userId);
                    }
                    else
                    {
                        var newTag = new Tag { Name = tagName, UserId = userId };
                        await _tagRepository.CreateAsync(newTag, cancellationToken);
                        tagEntities.Add(newTag);
                        _logger.LogDebug("Created new tag {TagName} for user {UserId}", tagName, userId);
                    }
                }
                
                _logger.LogInformation("Processed {TagCount} tags for user {UserId}", tagEntities.Count, userId);
                return tagEntities;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tag processing was cancelled for user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tags for user {UserId}", userId);
                throw;
            }
        }
    }
} 