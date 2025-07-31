using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NotesSolution.Application.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NotesSolution.Application.Interfaces;

namespace NotesSolution.Application.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly INoteRepository _noteRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<TagRequestDto> _validator;
        private readonly ILogger<TagService> _logger;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;

        public TagService(
            ITagRepository tagRepository,
            INoteRepository noteRepository,
            IMapper mapper,
            IValidator<TagRequestDto> validator,
            ILogger<TagService> logger,
            ICancellationTokenProvider cancellationTokenProvider)
        {
            _tagRepository = tagRepository;
            _noteRepository = noteRepository;
            _mapper = mapper;
            _validator = validator;
            _logger = logger;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public async Task<List<TagDto>> GetAllTags(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Getting all tags for user {UserId}", userId);
                
                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();
                
                var tags = await _tagRepository.GetAllAsync(userId, combinedToken);
                _logger.LogInformation("Found {Count} tags for user {UserId}", tags.Count, userId);
                return _mapper.Map<List<TagDto>>(tags);
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

        public async Task<TagDto?> GetTagById(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(10000); // 10 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Getting tag with id = {Id} for user {UserId}", id, userId);
                
                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();
                
                var tag = await GetUserTagByIdAsync(userId, id, combinedToken);
                if (tag == null)
                {
                    _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                    return null;
                }
                _logger.LogInformation("Tag with id {Id} found for user {UserId}", id, userId);
                return _mapper.Map<TagDto>(tag);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get tag by id operation was cancelled for user {UserId}, tag {Id}", userId, id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<TagDto?> GetTagByName(string userId, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(10000); // 10 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Getting tag with name = {Name} for user {UserId}", name, userId);
                
                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();
                
                var tag = await GetUserTagByNameAsync(userId, name, combinedToken);
                if (tag == null)
                {
                    _logger.LogWarning("Tag with name {Name} not found or not owned by user {UserId}", name, userId);
                    return null;
                }
                _logger.LogInformation("Tag with name {Name} found for user {UserId}", name, userId);
                return _mapper.Map<TagDto>(tag);
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

        public async Task<(TagDto? tag, List<string> errors, bool conflict)> CreateTag(string userId, TagRequestDto tagDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Attempting to create new tag for user {UserId} with name={Name}", userId, tagDto.Name);
                
                // Check cancellation before validation
                combinedToken.ThrowIfCancellationRequested();
                
                var validationResult = await _validator.ValidateAsync(tagDto, combinedToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for new tag: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false);
                }
                
                // Check cancellation before checking existing tag
                combinedToken.ThrowIfCancellationRequested();
                
                var existingTag = await _tagRepository.GetByNameAsync(userId, tagDto.Name, combinedToken);
                if (existingTag != null && existingTag.UserId == userId)
                {
                    _logger.LogWarning("Tag with name {Name} already exists for user {UserId}", tagDto.Name, userId);
                    return (null, new List<string>(), true);
                }
                
                var tag = new Tag { Name = tagDto.Name, UserId = userId };
                
                // Check cancellation before creating tag
                combinedToken.ThrowIfCancellationRequested();
                
                await _tagRepository.CreateAsync(tag, combinedToken);
                _logger.LogInformation("Created new tag with id {Id} for user {UserId}", tag.Id, userId);
                return (_mapper.Map<TagDto>(tag), new List<string>(), false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Create tag operation was cancelled for user {UserId}", userId);
                return (null, new List<string> { "Operation was cancelled" }, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag for user {UserId}", userId);
                return (null, new List<string> { "An error occurred while creating the tag" }, false);
            }
        }

        public async Task<(TagDto? tag, List<string> errors, bool notFound, bool conflict)> UpdateTag(string userId, Guid id, TagRequestDto tagDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Updating tag with id {Id} for user {UserId}", id, userId);
                
                // Check cancellation before validation
                combinedToken.ThrowIfCancellationRequested();
                
                var validationResult = await _validator.ValidateAsync(tagDto, combinedToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for updating tag {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false, false);
                }
                
                // Check cancellation before getting existing tag
                combinedToken.ThrowIfCancellationRequested();
                
                var existingTag = await GetUserTagByIdAsync(userId, id, combinedToken);
                if (existingTag == null)
                {
                    _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                    return (null, new List<string>(), true, false);
                }
                
                // Check cancellation before checking for name conflict
                combinedToken.ThrowIfCancellationRequested();
                
                var tagWithSameName = await GetUserTagByNameAsync(userId, tagDto.Name, combinedToken);
                if (tagWithSameName != null && tagWithSameName.Id != id)
                {
                    _logger.LogWarning("Tag with name {Name} already exists for user {UserId}", tagDto.Name, userId);
                    return (null, new List<string>(), false, true);
                }
                
                existingTag.Name = tagDto.Name;
                
                // Check cancellation before updating tag
                combinedToken.ThrowIfCancellationRequested();
                
                await _tagRepository.UpdateAsync(existingTag, combinedToken);
                _logger.LogInformation("Updated tag with id {Id} for user {UserId}", id, userId);
                return (_mapper.Map<TagDto>(existingTag), new List<string>(), false, false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update tag operation was cancelled for user {UserId}, tag {Id}", userId, id);
                return (null, new List<string> { "Operation was cancelled" }, false, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {Id} for user {UserId}", id, userId);
                return (null, new List<string> { "An error occurred while updating the tag" }, false, false);
            }
        }

        public async Task<bool> DeleteTag(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(30000); // 30 seconds timeout for complex operation
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Deleting tag with id {Id} for user {UserId}", id, userId);
                
                // Check cancellation before getting tag
                combinedToken.ThrowIfCancellationRequested();
                
                var tag = await GetUserTagByIdAsync(userId, id, combinedToken);
                if (tag == null)
                {
                    _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                    return false;
                }
                
                // Check cancellation before removing tag from notes
                combinedToken.ThrowIfCancellationRequested();
                
                await RemoveTagFromAllUserNotesAsync(userId, id, combinedToken);
                
                // Check cancellation before deleting tag
                combinedToken.ThrowIfCancellationRequested();
                
                await _tagRepository.RemoveAsync(tag, combinedToken);
                _logger.LogInformation("Deleted tag with id {Id} for user {UserId}", id, userId);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Delete tag operation was cancelled for user {UserId}, tag {Id}", userId, id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {Id} for user {UserId}", id, userId);
                return false;
            }
        }

        private async Task<Tag?> GetUserTagByIdAsync(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            return await _tagRepository.GetAsync(userId, id, cancellationToken);
        }

        private async Task<Tag?> GetUserTagByNameAsync(string userId, string name, CancellationToken cancellationToken = default)
        {
            return await _tagRepository.GetByNameAsync(userId, name, cancellationToken);
        }

        private async Task RemoveTagFromAllUserNotesAsync(string userId, Guid tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notes = await _noteRepository.GetAllAsync(userId, null, null, null, null, 1, int.MaxValue, cancellationToken);
                foreach (var note in notes.Where(n => n.Tags.Any(t => t.Id == tagId)))
                {
                    // Check cancellation before each note update
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    note.Tags.RemoveAll(t => t.Id == tagId);
                    await _noteRepository.UpdateAsync(note, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Remove tag from notes operation was cancelled for user {UserId}, tag {Id}", userId, tagId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {Id} from notes for user {UserId}", tagId, userId);
                throw;
            }
        }
    }
} 