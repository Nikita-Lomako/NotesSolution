using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using System.Net;
using Microsoft.Extensions.Logging;

namespace NotesSolution.API.Endpoints
{
    public static class NoteEndpoints
    {
        public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/notes").WithTags("Notes");

            group.MapGet("/", GetAllNotes).WithName("GetAllNotes").Produces<List<NoteDto>>(200);
            group.MapGet("/{id}", GetNoteById).WithName("GetNoteById").Produces<NoteDto>(200).Produces(404);
            group.MapPost("/", CreateNote).WithName("CreateNote").Accepts<NoteCreateDto>("application/json").Produces<NoteDto>(201).Produces(400);
            group.MapPut("/{id}", UpdateNote).WithName("UpdateNote").Accepts<NoteUpdateDto>("application/json").Produces<NoteDto>(200).Produces(400).Produces(404);
            group.MapDelete("/{id}", DeleteNote).WithName("DeleteNote").Produces(204).Produces(404);
        }

        private static async Task<IResult> GetAllNotes(INoteRepository noteRepository, IMapper mapper, ILogger<Program> logger,
            [FromQuery] string? search, [FromQuery] string? tag, [FromQuery] string? sort, [FromQuery] string? order,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            logger.LogInformation("Getting all notes with parameters: search={Search}, tag={Tag}, sort={Sort}, order={Order}, page={Page}, pageSize={PageSize}", search, tag, sort, order, page, pageSize);
            var notes = await noteRepository.GetAllAsync(search, tag, sort, order, page, pageSize);
            var noteDtos = mapper.Map<List<NoteDto>>(notes);
            return Results.Ok(noteDtos);
        }

        private static async Task<IResult> GetNoteById(Guid id, INoteRepository noteRepository, IMapper mapper, ILogger<Program> logger)
        {
            logger.LogInformation($"Getting note with id = {id}");
            var note = await noteRepository.GetAsync(id);
            if (note == null)
            {
                logger.LogWarning($"Note with id {id} not found");
                return Results.NotFound();
            }
            var noteDto = mapper.Map<NoteDto>(note);
            return Results.Ok(noteDto);
        }

        private static async Task<IResult> CreateNote(
            NoteCreateDto noteDto,
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IMapper mapper,
            IValidator<NoteCreateDto> validator,
            ILogger<Program> logger)
        {
            logger.LogInformation("Attempting to create new note");
            var validationResult = await validator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for new note: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var note = mapper.Map<Note>(noteDto);

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
                    var newTag = new Tag { Name = tagName };
                    await tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            note.Tags = tagEntities;
            note.CreationDate = DateTime.UtcNow;
            
            await noteRepository.CreateAsync(note);
            await noteRepository.SaveAsync();

            var createdNoteDto = mapper.Map<NoteDto>(note);
            logger.LogInformation($"Created new note with id {note.Id}");
            return Results.Created($"/api/notes/{createdNoteDto.Id}", createdNoteDto);
        }

        private static async Task<IResult> UpdateNote(
            Guid id,
            NoteUpdateDto noteDto,
            INoteRepository noteRepository,
            ITagRepository tagRepository,
            IMapper mapper,
            IValidator<NoteUpdateDto> validator,
            ILogger<Program> logger)
        {
            logger.LogInformation($"Updating note with id {id}");
            var validationResult = await validator.ValidateAsync(noteDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for updating note {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var existingNote = await noteRepository.GetAsync(id);
            if (existingNote == null)
            {
                logger.LogWarning($"Note with id {id} not found");
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
                    var newTag = new Tag { Name = tagName };
                    await tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            
            mapper.Map(noteDto, existingNote);
            existingNote.Tags = tagEntities;

            await noteRepository.UpdateAsync(existingNote);
            await noteRepository.SaveAsync();

            var updatedNoteDto = mapper.Map<NoteDto>(existingNote);
            logger.LogInformation($"Updated note with id {id}");
            return Results.Ok(updatedNoteDto);
        }

        private static async Task<IResult> DeleteNote(Guid id, INoteRepository noteRepository, ILogger<Program> logger)
        {
            logger.LogInformation($"Deleting note with id {id}");
            var existingNote = await noteRepository.GetAsync(id);
            if (existingNote == null)
            {
                logger.LogWarning($"Note with id {id} not found");
                return Results.NotFound();
            }
            await noteRepository.RemoveAsync(existingNote);
            logger.LogInformation($"Note with id {id} deleted");
            return Results.NoContent();
        }
    }
} 