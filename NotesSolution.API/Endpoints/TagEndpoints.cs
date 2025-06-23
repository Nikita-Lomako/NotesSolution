using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace NotesSolution.API.Endpoints
{
    public static class TagEndpoints
    {
        public static void MapTagEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tags").WithTags("Tags");

            group.MapGet("/", GetAllTags).WithName("GetAllTags")
                .Produces<List<TagDto>>(200).RequireAuthorization();
            group.MapGet("/{id}", GetTagById).WithName("GetTagById").Produces<TagDto>(200).Produces(404).RequireAuthorization();
            group.MapGet("/by-name/{name}", GetTagByName).WithName("GetTagByName").Produces<TagDto>(200).Produces(404).RequireAuthorization();

            group.MapPost("/", CreateTag).WithName("CreateTag")
                .Accepts<TagDto>("application/json").Produces<TagDto>(201).Produces(400).RequireAuthorization();

            group.MapPut("/{id}", UpdateTag).WithName("UpdateTag")
                .Accepts<TagDto>("application/json").Produces<TagDto>(200).Produces(400).Produces(404).RequireAuthorization();

            group.MapDelete("/{id}", DeleteTag).WithName("DeleteTag").Produces(204).Produces(404).RequireAuthorization();

        }

        private static async Task<IResult> GetAllTags(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation("Getting all tags");
            var tags = await tagRepository.GetAllAsync();
            tags = tags.Where(t => t.UserId == userId).ToList();
            var tagDtos = mapper.Map<List<TagDto>>(tags);
            return Results.Ok(tagDtos);
        }

        private static async Task<IResult> CreateTag(
            TagDto tagDto,
            ITagRepository tagRepository,
            IValidator<TagDto> validator,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation("Attempting to create new tag");
            var validationResult = await validator.ValidateAsync(tagDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for new tag: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var existingTag = await tagRepository.GetByNameAsync(tagDto.Name);
            if (existingTag != null && existingTag.UserId == userId)
            {
                logger.LogWarning("Tag with name {Name} already exists", tagDto.Name);
                return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
            }
            var tag = mapper.Map<Tag>(tagDto);
            tag.UserId = userId;
            await tagRepository.CreateAsync(tag);
            await tagRepository.SaveAsync();
            var createdTagDto = mapper.Map<TagDto>(tag);
            logger.LogInformation("Created new tag with id {Id}", tag.Id);
            return Results.Created($"/api/tags/{createdTagDto.Id}", createdTagDto);
        }

        private static async Task<IResult> UpdateTag(
            Guid id,
            TagDto tagDto,
            ITagRepository tagRepository,
            IValidator<TagDto> validator,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation($"Updating tag with id {id}");
            var validationResult = await validator.ValidateAsync(tagDto);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Validation failed for updating tag {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Results.BadRequest(validationResult.Errors);
            }
            var existingTag = await tagRepository.GetAsync(id);
            if (existingTag == null || existingTag.UserId != userId)
            {
                logger.LogWarning($"Tag with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            var tagWithSameName = await tagRepository.GetByNameAsync(tagDto.Name);
            if (tagWithSameName != null && tagWithSameName.Id != id && tagWithSameName.UserId == userId)
            {
                logger.LogWarning("Tag with name {Name} already exists", tagDto.Name);
                return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
            }
            existingTag.Name = tagDto.Name;
            await tagRepository.UpdateAsync(existingTag);
            await tagRepository.SaveAsync();
            var updatedTagDto = mapper.Map<TagDto>(existingTag);
            logger.LogInformation($"Updated tag with id {id}");
            return Results.Ok(updatedTagDto);
        }

        private static async Task<IResult> DeleteTag(
            Guid id,
            ITagRepository tagRepository,
            INoteRepository noteRepository,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation($"Deleting tag with id {id}");
            var tag = await tagRepository.GetAsync(id);
            if (tag == null || tag.UserId != userId)
            {
                logger.LogWarning($"Tag with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            // Remove tag from all notes
            var notes = await noteRepository.GetAllAsync(null, null, null, null, 1, int.MaxValue);
            foreach (var note in notes.Where(n => n.UserId == userId))
            {
                note.Tags.RemoveAll(t => t.Id == id);
                await noteRepository.UpdateAsync(note);
            }
            await tagRepository.RemoveAsync(tag);
            await tagRepository.SaveAsync();
            logger.LogInformation($"Deleted tag with id {id}");
            return Results.NoContent();
        }

        private static async Task<IResult> GetTagById(
            Guid id,
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation($"Getting tag with id = {id}");
            var tag = await tagRepository.GetAsync(id);
            if (tag == null || tag.UserId != userId)
            {
                logger.LogWarning($"Tag with id {id} not found or not owned by user");
                return Results.NotFound();
            }
            var tagDto = mapper.Map<TagDto>(tag);
            return Results.Ok(tagDto);
        }

        private static async Task<IResult> GetTagByName(
            string name,
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            logger.LogInformation($"Getting tag with name = {name}");
            var tag = await tagRepository.GetByNameAsync(name);
            if (tag == null || tag.UserId != userId)
            {
                logger.LogWarning($"Tag with name {name} not found or not owned by user");
                return Results.NotFound();
            }
            var tagDto = mapper.Map<TagDto>(tag);
            return Results.Ok(tagDto);
        }
    }
} 