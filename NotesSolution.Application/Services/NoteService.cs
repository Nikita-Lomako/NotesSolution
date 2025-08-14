using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NotesSolution.Application.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Core.Interfaces;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using NotesSolution.Application.Interfaces;
using System.Text.Json;

namespace NotesSolution.Application.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;
        private readonly IValidator<NoteCreateDto> _createValidator;
        private readonly IValidator<NoteUpdateDto> _updateValidator;
        private readonly ILogger<NoteService> _logger;
        private readonly ITagHelperService _tagHelperService;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IDistributedCache _cache;

        public NoteService(
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IImageService imageService,
            IMapper mapper,
            IValidator<NoteCreateDto> createValidator,
            IValidator<NoteUpdateDto> updateValidator,
            ILogger<NoteService> logger,
            ITagHelperService tagHelperService,
            ICancellationTokenProvider cancellationTokenProvider,
            IDistributedCache cache)
        {
            _noteRepository = noteRepository;
            _tagRepository = tagRepository;
            _imageService = imageService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
            _tagHelperService = tagHelperService;
            _cancellationTokenProvider = cancellationTokenProvider;
            _cache = cache;
        }

        public async Task<List<NoteDto>> GetAllNotes(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"notes_{userId}_{search}_{tag}_{sort}_{order}_{page}_{pageSize}";
            string? cachedNotesJson = null;

            try
            {
                cachedNotesJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache. Caching is disabled for this request.");
            }

            if (!string.IsNullOrEmpty(cachedNotesJson))
            {
                _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<List<NoteDto>>(cachedNotesJson);
            }

            _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
            try
            {
                // Use linked token source to combine request token with timeout
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(30000); // 30 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutTokenSource.Token,
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Getting all notes for user {UserId} with search={Search}, tag={Tag}, sort={Sort}, order={Order}, page={Page}, pageSize={PageSize}",
                    userId, search, tag, sort, order, page, pageSize);

                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();

                var notes = await _noteRepository.GetAllAsync(userId, search, tag, sort, order, page, pageSize, combinedToken);

                _logger.LogInformation("Found {Count} notes for user {UserId}", notes.Count, userId);
                var noteDtos = _mapper.Map<List<NoteDto>>(notes);

                var notesJson = JsonSerializer.Serialize(noteDtos);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                try
                {
                    await _cache.SetStringAsync(cacheKey, notesJson, cacheOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to Redis cache. Caching is disabled for this request.");
                }

                return noteDtos;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was cancelled for user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for user {UserId}", userId);
                throw;
            }
        }

        private async Task<Note?> GetUserNoteByIdAsync(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(15000); // 15 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;
                combinedToken.ThrowIfCancellationRequested();

                return await _noteRepository.GetAsync(userId, id, combinedToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get note operation was cancelled for user {UserId}, note {Id}", userId, id);
                throw;
            }
        }

        public async Task<NoteDto?> GetNoteById(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"note_{userId}_{id}";
            string? cachedNoteJson = null;

            try
            {
                cachedNoteJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache. Caching is disabled for this request.");
            }

            if (!string.IsNullOrEmpty(cachedNoteJson))
            {
                _logger.LogInformation("Cache hit for note with id {Id}", id);
                var cachedNote = JsonSerializer.Deserialize<NoteDto>(cachedNoteJson);
                if (cachedNote is not null)
                {
                    return cachedNote;
                }
            }

            _logger.LogInformation("Cache miss for note with id {Id}", id);
            try
            {
                _logger.LogInformation("Getting note with id = {Id} for user {UserId}", id, userId);
                var note = await GetUserNoteByIdAsync(userId, id, cancellationToken);
                if (note == null)
                {
                    _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                    return null;
                }
                _logger.LogInformation("Note with id {Id} found for user {UserId}", id, userId);
                var noteDto = _mapper.Map<NoteDto>(note);

                var noteJson = JsonSerializer.Serialize(noteDto);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                try
                {
                    await _cache.SetStringAsync(cacheKey, noteJson, cacheOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to Redis cache. Caching is disabled for this request.");
                }

                return noteDto;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get note by id operation was cancelled for user {UserId}, note {Id}", userId, id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<(NoteDto? note, List<string> errors)> CreateNote(string userId, NoteCreateDto noteDto, IFormFileCollection? images, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(60000); // 60 seconds timeout for image processing
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Attempting to create new note for user {UserId} with name={Name}", userId, noteDto.Name);
                
                // Check cancellation before validation
                combinedToken.ThrowIfCancellationRequested();
                
                var validationResult = await _createValidator.ValidateAsync(noteDto, combinedToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for new note: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList());
                }
            var note = _mapper.Map<Note>(noteDto);

                note.UserId = userId;

                // Check cancellation before tag processing
                combinedToken.ThrowIfCancellationRequested();
                var tagEntities = await _tagHelperService.GetOrCreateTagsAsync(noteDto.Tags, userId, combinedToken);

                note.Tags = tagEntities;
                note.CreationDate = DateTime.UtcNow;
                var imageHashes = new HashSet<string>();
                
                if (images != null && images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        // Check cancellation before each image processing
                        combinedToken.ThrowIfCancellationRequested();
                        
                        var hash = await _imageService.ComputeImageHashAsync(image, combinedToken);
                        if (!imageHashes.Contains(hash))
                        {
                            var imageUrl = await _imageService.SaveImageAsync(image, combinedToken);
                            note.ImageUrls.Add(imageUrl);
                            imageHashes.Add(hash);
                        }
                    }
                }

                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();
                await _noteRepository.CreateAsync(note, combinedToken);
                
                _logger.LogInformation("Created new note with id {Id} for user {UserId}", note.Id, userId);
                return (_mapper.Map<NoteDto>(note), new List<string>());
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Create note operation was cancelled for user {UserId}", userId);
                errors.Add("Operation was cancelled");
                return (null, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note for user {UserId}", userId);
                errors.Add("An error occurred while creating the note");
                return (null, errors);
            }
        }

        public async Task<(NoteDto? note, List<string> errors, bool notFound)> UpdateNote(string userId, Guid id, NoteUpdateDto noteDto, IFormFileCollection? images, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(60000); // 60 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Updating note with id {Id} for user {UserId}", id, userId);
                
                // Check cancellation before validation
                combinedToken.ThrowIfCancellationRequested();
                
                var validationResult = await _updateValidator.ValidateAsync(noteDto, combinedToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for updating note {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false);
                }

                var existingNote = await GetUserNoteByIdAsync(userId, id, combinedToken);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                    return (null, new List<string>(), true);
                }
                
                // Check cancellation before tag processing
                combinedToken.ThrowIfCancellationRequested();
                var tagEntities = await _tagHelperService.GetOrCreateTagsAsync(noteDto.Tags, userId, combinedToken);

                _mapper.Map(noteDto, existingNote);
                existingNote.Tags = tagEntities;
                
                // Safely delete old images
                if (existingNote.ImageUrls != null && existingNote.ImageUrls.Count > 0)
                {
                    foreach (var imageUrl in existingNote.ImageUrls)
                    {
                        try
                        {
                            _imageService.DeleteImage(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old image {ImageUrl}", imageUrl);
                        }
                    }
                    existingNote.ImageUrls.Clear();
                }
                
                var imageHashes = new HashSet<string>();
                if (images != null && images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        // Check cancellation before each image processing
                        combinedToken.ThrowIfCancellationRequested();
                        
                        var hash = await _imageService.ComputeImageHashAsync(image, combinedToken);
                        if (!imageHashes.Contains(hash))
                        {
                            var imageUrl = await _imageService.SaveImageAsync(image, combinedToken);
                            existingNote.ImageUrls?.Add(imageUrl);
                            imageHashes.Add(hash);
                        }
                    }
                }

                // Check cancellation before database operations
                combinedToken.ThrowIfCancellationRequested();
                await _noteRepository.UpdateAsync(existingNote, combinedToken);
                await _noteRepository.SaveAsync(combinedToken);

                var cacheKey = $"note_{userId}_{id}";
                try
                {
                    await _cache.RemoveAsync(cacheKey, cancellationToken);
                    _logger.LogInformation("Cache invalidated for note with id {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache for note {Id}", id);
                }
                
                _logger.LogInformation("Updated note with id {Id} for user {UserId}", id, userId);
                return (_mapper.Map<NoteDto>(existingNote), new List<string>(), false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update note operation was cancelled for user {UserId}, note {Id}", userId, id);
                errors.Add("Operation was cancelled");
                return (null, errors, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {Id} for user {UserId}", id, userId);
                errors.Add("An error occurred while updating the note");
                return (null, errors, false);
            }
        }

        public async Task<bool> DeleteNote(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(30000); // 30 seconds timeout
                using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
                    cancellationToken, 
                    timeoutTokenSource.Token, 
                    _cancellationTokenProvider.GetDefaultToken());

                var combinedToken = linkedTokenSource.Token;

                _logger.LogInformation("Deleting note with id {Id} for user {UserId}", id, userId);
                
                // Check cancellation before getting note
                combinedToken.ThrowIfCancellationRequested();
                
                var existingNote = await GetUserNoteByIdAsync(userId, id, combinedToken);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                    return false;
                }

                var imageUrls = existingNote.ImageUrls?.ToList() ?? new List<string>();
                
                // Check cancellation before database operation
                combinedToken.ThrowIfCancellationRequested();
                await _noteRepository.RemoveAsync(existingNote, combinedToken);

                var cacheKey = $"note_{userId}_{id}";
                try
                {
                    await _cache.RemoveAsync(cacheKey, cancellationToken);
                    _logger.LogInformation("Cache invalidated for note with id {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache for note {Id}", id);
                }
                
                // Safely delete images after successful database operation
                foreach (var imageUrl in imageUrls)
                {
                    try
                    {
                        _imageService.DeleteImage(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete image {ImageUrl} for note {Id}", imageUrl, id);
                    }
                }
                
                _logger.LogInformation("Note with id {Id} deleted for user {UserId}", id, userId);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Delete note operation was cancelled for user {UserId}, note {Id}", userId, id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {Id} for user {UserId}", id, userId);
                return false;
            }
        }       
    }
} 