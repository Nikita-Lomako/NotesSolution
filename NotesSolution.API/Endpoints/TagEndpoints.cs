using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;

namespace NotesSolution.API.Endpoints
{
    public static class TagEndpoints
    {
        public static void MapTagEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tags").WithTags("Tags");

            group.MapGet("/", GetAllTags).WithName("GetAllTags").Produces<List<TagDto>>(200);
            
            group.MapPost("/", CreateTag).WithName("CreateTag").Accepts<TagCreateDto>("application/json").Produces<TagDto>(201).Produces(400);
        }

        private static async Task<IResult> GetAllTags(ITagRepository tagRepository, IMapper mapper, ILogger<Program> logger)
        {
            logger.LogInformation("Getting all tags");
            var tags = await tagRepository.GetAllAsync();
            var tagDtos = mapper.Map<List<TagDto>>(tags);
            return Results.Ok(tagDtos);
        }

        private static async Task<IResult> CreateTag(
            TagCreateDto tagDto,
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<Program> logger)
        {
            logger.LogInformation("Attempting to create new tag");
            
            var existingTag = await tagRepository.GetByNameAsync(tagDto.Name);
            if (existingTag != null)
            {
                logger.LogWarning("Tag with name {Name} already exists", tagDto.Name);
                return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
            }

            var tag = mapper.Map<Tag>(tagDto);

            await tagRepository.CreateAsync(tag);
            await tagRepository.SaveAsync();
            
            var createdTagDto = mapper.Map<TagDto>(tag);
            logger.LogInformation("Created new tag with id {Id}", tag.Id);
            return Results.Created($"/api/tags/{createdTagDto.Id}", createdTagDto);
        }
    }
} 