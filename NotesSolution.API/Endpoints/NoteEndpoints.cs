using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using NotesSolution.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace NotesSolution.API.Endpoints
{
    public static class NoteEndpoints
    {
        public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/notes").WithTags("Notes");

            group.MapGet("/", GetAllNotes).WithName("GetAllNotes")
                .Produces<List<NoteDto>>(200).RequireAuthorization();
            group.MapGet("/{id}", GetNoteById).WithName("GetNoteById")
                .Produces<NoteDto>(200).Produces(404).RequireAuthorization();

            group.MapPost("/", CreateNote).WithName("CreateNote")
                .Accepts<NoteCreateDto>("multipart/form-data")
                .Produces<NoteDto>(201).Produces(400).RequireAuthorization().DisableAntiforgery();

            group.MapPut("/{id}", UpdateNote).WithName("UpdateNote")
                .Accepts<NoteUpdateDto>("multipart/form-data")
                .Produces<NoteDto>(200).Produces(400).Produces(404).RequireAuthorization().DisableAntiforgery();

            group.MapDelete("/{id}", DeleteNote).WithName("DeleteNote")
                .Produces(204).Produces(404).RequireAuthorization();
        }

        private static async Task<IResult> GetAllNotes(
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromQuery] string? search, [FromQuery] string? tag, [FromQuery] string? sort, [FromQuery] string? order,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("Getting all notes with parameters: search={Search}, tag={Tag}, sort={Sort}, order={Order}, page={Page}, pageSize={PageSize}", search, tag, sort, order, page, pageSize);
            var notes = await noteRepository.GetAllAsync(search, tag, sort, order, page, pageSize);
            notes = notes.Where(n => n.UserId == userId).ToList();
            var noteDtos = mapper.Map<List<NoteDto>>(notes);
            return Results.Ok(noteDtos);
        }

        private static async Task<IResult> GetNoteById(
            Guid id,
            INoteRepository noteRepository,
            IMapper mapper, ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Getting note with id = {id}");
            var note = await noteRepository.GetAsync(id);
            if (note == null || note.UserId != userId)
            {
                logger.LogWarning($"Note with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            var noteDto = mapper.Map<NoteDto>(note);
            return Results.Ok(noteDto);
        }

        private static List<string> NormalizeTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return new List<string>();
            return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static async Task<IResult> CreateNote(
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string tags,
            [FromForm] IFormFileCollection? images,
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IImageService imageService,
            IMapper mapper,
            IValidator<NoteCreateDto> validator,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("Attempting to create new note");
            var normalizedTags = NormalizeTags(tags);
            var noteDto = new NoteCreateDto
            {
                Name = name,
                Description = description,
                Tags = normalizedTags,
                ImageUrls = new List<string>()
            };
            var validationResult = await validator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for new note: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var note = mapper.Map<Note>(noteDto);
            note.UserId = userId;
            var tagEntities = new List<Tag>();
            foreach (var tagName in noteDto.Tags)
            {
                var existingTag = await tagRepository.GetByNameAsync(tagName);
                if (existingTag != null)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName, UserId = userId };
                    await tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            note.Tags = tagEntities;
            note.CreationDate = DateTime.UtcNow;
            // Handle image uploads
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    var imageUrl = await imageService.SaveImageAsync(image);
                    note.ImageUrls.Add(imageUrl);
                }
            }
            await noteRepository.CreateAsync(note);
            await noteRepository.SaveAsync();
            var createdNoteDto = mapper.Map<NoteDto>(note);
            logger.LogInformation($"Created new note with id {note.Id}");
            return Results.Created($"/api/notes/{createdNoteDto.Id}", createdNoteDto);
        }

        private static async Task<IResult> UpdateNote(
            Guid id,
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string tags,
            [FromForm] IFormFileCollection? images,
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IImageService imageService,
            IMapper mapper,
            IValidator<NoteUpdateDto> validator,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Updating note with id {id}");
            var normalizedTags = NormalizeTags(tags);
            var noteDto = new NoteUpdateDto
            {
                Name = name,
                Description = description,
                Tags = normalizedTags,
                ImageUrls = new List<string>()
            };
            var validationResult = await validator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for updating note {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var existingNote = await noteRepository.GetAsync(id);
            if (existingNote == null || existingNote.UserId != userId)
            {
                logger.LogWarning($"Note with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            var tagEntities = new List<Tag>();
            foreach (var tagName in noteDto.Tags)
            {
                var existingTag = await tagRepository.GetByNameAsync(tagName);
                if (existingTag != null)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName, UserId = userId };
                    await tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            mapper.Map(noteDto, existingNote);
            existingNote.Tags = tagEntities;
            // Handle image uploads
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    var imageUrl = await imageService.SaveImageAsync(image);
                    existingNote.ImageUrls.Add(imageUrl);
                }
            }
            await noteRepository.UpdateAsync(existingNote);
            await noteRepository.SaveAsync();
            var updatedNoteDto = mapper.Map<NoteDto>(existingNote);
            logger.LogInformation($"Updated note with id {id}");
            return Results.Ok(updatedNoteDto);
        }

        private static async Task<IResult> DeleteNote(Guid id, INoteRepository noteRepository, ILogger<Program> logger, [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Deleting note with id {id}");
            var existingNote = await noteRepository.GetAsync(id);
            if (existingNote == null || existingNote.UserId != userId)
            {
                logger.LogWarning($"Note with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            await noteRepository.RemoveAsync(existingNote);
            logger.LogInformation($"Note with id {id} deleted");
            return Results.NoContent();
        }
    }
}