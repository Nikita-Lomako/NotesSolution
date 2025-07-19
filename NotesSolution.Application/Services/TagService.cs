using System;
using System.Collections.Generic;
using System.Linq;
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

        public TagService(
            ITagRepository tagRepository,
            INoteRepository noteRepository,
            IMapper mapper,
            IValidator<TagRequestDto> validator,
            ILogger<TagService> logger)
        {
            _tagRepository = tagRepository;
            _noteRepository = noteRepository;
            _mapper = mapper;
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<TagDto>> GetAllTags(string userId)
        {
            _logger.LogInformation("Getting all tags for user {UserId}", userId);
            var tags = await _tagRepository.GetAllAsync(userId);
            _logger.LogInformation("Found {Count} tags for user {UserId}", tags.Count, userId);
            return _mapper.Map<List<TagDto>>(tags);
        }

        public async Task<TagDto?> GetTagById(string userId, Guid id)
        {
            _logger.LogInformation("Getting tag with id = {Id} for user {UserId}", id, userId);
            var tag = await GetUserTagByIdAsync(userId, id);
            if (tag == null)
            {
                _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                return null;
            }
            _logger.LogInformation("Tag with id {Id} found for user {UserId}", id, userId);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<TagDto?> GetTagByName(string userId, string name)
        {
            _logger.LogInformation("Getting tag with name = {Name} for user {UserId}", name, userId);
            var tag = await GetUserTagByNameAsync(userId, name);
            if (tag == null)
            {
                _logger.LogWarning("Tag with name {Name} not found or not owned by user {UserId}", name, userId);
                return null;
            }
            _logger.LogInformation("Tag with name {Name} found for user {UserId}", name, userId);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<(TagDto? tag, List<string> errors, bool conflict)> CreateTag(string userId, TagRequestDto tagDto)
        {
            _logger.LogInformation("Attempting to create new tag for user {UserId} with name={Name}", userId, tagDto.Name);
            var validationResult = await _validator.ValidateAsync(tagDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for new tag: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false);
            }
            var existingTag = await _tagRepository.GetByNameAsync(userId, tagDto.Name);
            if (existingTag != null && existingTag.UserId == userId)
            {
                _logger.LogWarning("Tag with name {Name} already exists for user {UserId}", tagDto.Name, userId);
                return (null, new List<string>(), true);
            }
            var tag = new Tag { Name = tagDto.Name, UserId = userId };
            await _tagRepository.CreateAsync(tag);
            _logger.LogInformation("Created new tag with id {Id} for user {UserId}", tag.Id, userId);
            return (_mapper.Map<TagDto>(tag), new List<string>(), false);
        }

        public async Task<(TagDto? tag, List<string> errors, bool notFound, bool conflict)> UpdateTag(string userId, Guid id, TagRequestDto tagDto)
        {
            _logger.LogInformation("Updating tag with id {Id} for user {UserId}", id, userId);
            var validationResult = await _validator.ValidateAsync(tagDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for updating tag {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return (null, validationResult.Errors.Select(e => e.ErrorMessage).ToList(), false, false);
            }
            var existingTag = await GetUserTagByIdAsync(userId, id);
            if (existingTag == null)
            {
                _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                return (null, new List<string>(), true, false);
            }
            var tagWithSameName = await GetUserTagByNameAsync(userId, tagDto.Name);
            if (tagWithSameName != null && tagWithSameName.Id != id)
            {
                _logger.LogWarning("Tag with name {Name} already exists for user {UserId}", tagDto.Name, userId);
                return (null, new List<string>(), false, true);
            }
            existingTag.Name = tagDto.Name;
            await _tagRepository.UpdateAsync(existingTag);
            _logger.LogInformation("Updated tag with id {Id} for user {UserId}", id, userId);
            return (_mapper.Map<TagDto>(existingTag), new List<string>(), false, false);
        }

        public async Task<bool> DeleteTag(string userId, Guid id)
        {
            _logger.LogInformation("Deleting tag with id {Id} for user {UserId}", id, userId);
            var tag = await GetUserTagByIdAsync(userId, id);
            if (tag == null)
            {
                _logger.LogWarning("Tag with id {Id} not found or not owned by user {UserId}", id, userId);
                return false;
            }
            await RemoveTagFromAllUserNotesAsync(userId, id);
            await _tagRepository.RemoveAsync(tag);
            _logger.LogInformation("Deleted tag with id {Id} for user {UserId}", id, userId);
            return true;
        }

        private async Task<Tag?> GetUserTagByIdAsync(string userId, Guid id)
        {
            return await _tagRepository.GetAsync(userId, id);
        }

        private async Task<Tag?> GetUserTagByNameAsync(string userId, string name)
        {
            return await _tagRepository.GetByNameAsync(userId, name);
        }

        private async Task RemoveTagFromAllUserNotesAsync(string userId, Guid tagId)
        {
            var notes = await _noteRepository.GetAllAsync(userId, null, null, null, null, 1, int.MaxValue);
            foreach (var note in notes.Where(n => n.Tags.Any(t => t.Id == tagId)))
            {
                note.Tags.RemoveAll(t => t.Id == tagId);
                await _noteRepository.UpdateAsync(note);
            }
        }
    }
} 