using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using NotesSolution.Core.Interfaces;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace NotesSolution.API.Services
{
    public class NoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;
        private readonly IValidator<NoteCreateDto> _createValidator;
        private readonly IValidator<NoteUpdateDto> _updateValidator;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IImageService imageService,
            IMapper mapper,
            IValidator<NoteCreateDto> createValidator,
            IValidator<NoteUpdateDto> updateValidator,
            ILogger<NoteService> logger)
        {
            _noteRepository = noteRepository;
            _tagRepository = tagRepository;
            _imageService = imageService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        public async Task<List<NoteDto>> GetAllNotes(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize)
        {
            _logger.LogInformation("Getting all notes for user {UserId} with search={Search}, tag={Tag}, sort={Sort}, order={Order}, page={Page}, pageSize={PageSize}", userId, search, tag, sort, order, page, pageSize);
            var notes = await _noteRepository.GetAllAsync(search, tag, sort, order, page, pageSize);
            notes = notes.Where(n => n.UserId == userId).ToList();
            _logger.LogInformation("Found {Count} notes for user {UserId}", notes.Count, userId);
            return _mapper.Map<List<NoteDto>>(notes);
        }

        public async Task<NoteDto?> GetNoteById(string userId, Guid id)
        {
            _logger.LogInformation("Getting note with id = {Id} for user {UserId}", id, userId);
            var note = await _noteRepository.GetAsync(id);
            if (note == null || note.UserId != userId)
            {
                _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                return null;
            }
            _logger.LogInformation("Note with id {Id} found for user {UserId}", id, userId);
            return _mapper.Map<NoteDto>(note);
        }

        public async Task<(NoteDto? note, List<string> errors)> CreateNote(string userId, NoteCreateDto noteDto, IFormFileCollection? images)
        {
            _logger.LogInformation("Attempting to create new note for user {UserId} with name={Name}", userId, noteDto.Name);
            var validationResult = await _createValidator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for new note: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList());
            }
            var note = _mapper.Map<Note>(noteDto);
            note.UserId = userId;
            var tagEntities = new List<Tag>();
            foreach (var tagName in noteDto.Tags)
            {
                var existingTag = await _tagRepository.GetByNameAsync(tagName);
                if (existingTag != null && existingTag.UserId == userId)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName, UserId = userId };
                    await _tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            note.Tags = tagEntities;
            note.CreationDate = DateTime.UtcNow;
            var imageHashes = new HashSet<string>();
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    var hash = await _imageService.ComputeImageHashAsync(image);
                    if (!imageHashes.Contains(hash))
                    {
                        var imageUrl = await _imageService.SaveImageAsync(image);
                        note.ImageUrls.Add(imageUrl);
                        imageHashes.Add(hash);
                    }
                }
            }
            await _noteRepository.CreateAsync(note);
            await _noteRepository.SaveAsync();
            _logger.LogInformation("Created new note with id {Id} for user {UserId}", note.Id, userId);
            return (_mapper.Map<NoteDto>(note), new List<string>());
        }

        public async Task<(NoteDto? note, List<string> errors, bool notFound)> UpdateNote(string userId, Guid id, NoteUpdateDto noteDto, IFormFileCollection? images)
        {
            _logger.LogInformation("Updating note with id {Id} for user {UserId}", id, userId);
            var validationResult = await _updateValidator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for updating note {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false);
            }
            var existingNote = await _noteRepository.GetAsync(id);
            if (existingNote == null || existingNote.UserId != userId)
            {
                _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                return (null, new List<string>(), true);
            }
            var tagEntities = new List<Tag>();
            foreach (var tagName in noteDto.Tags)
            {
                var existingTag = await _tagRepository.GetByNameAsync(tagName);
                if (existingTag != null && existingTag.UserId == userId)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName, UserId = userId };
                    await _tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            _mapper.Map(noteDto, existingNote);
            existingNote.Tags = tagEntities;
            if (existingNote.ImageUrls != null && existingNote.ImageUrls.Count > 0)
            {
                foreach (var imageUrl in existingNote.ImageUrls)
                {
                    _imageService.DeleteImage(imageUrl);
                }
                existingNote.ImageUrls.Clear();
            }
            var imageHashes = new HashSet<string>();
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    var hash = await _imageService.ComputeImageHashAsync(image);
                    if (!imageHashes.Contains(hash))
                    {
                        var imageUrl = await _imageService.SaveImageAsync(image);
                        existingNote.ImageUrls.Add(imageUrl);
                        imageHashes.Add(hash);
                    }
                }
            }
            await _noteRepository.UpdateAsync(existingNote);
            await _noteRepository.SaveAsync();
            _logger.LogInformation("Updated note with id {Id} for user {UserId}", id, userId);
            return (_mapper.Map<NoteDto>(existingNote), new List<string>(), false);
        }

        public async Task<bool> DeleteNote(string userId, Guid id)
        {
            _logger.LogInformation("Deleting note with id {Id} for user {UserId}", id, userId);
            var existingNote = await _noteRepository.GetAsync(id);
            if (existingNote == null || existingNote.UserId != userId)
            {
                _logger.LogWarning("Note with id {Id} not found or not owned by user {UserId}", id, userId);
                return false;
            }
            var imageUrls = existingNote.ImageUrls?.ToList() ?? new List<string>();
            await _noteRepository.RemoveAsync(existingNote);
            foreach (var imageUrl in imageUrls)
            {
                _imageService.DeleteImage(imageUrl);
            }
            _logger.LogInformation("Note with id {Id} deleted for user {UserId}", id, userId);
            return true;
        }
    }
}
