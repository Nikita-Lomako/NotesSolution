using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using NotesSolution.API.Services;

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
                .Accepts<TagRequestDto>("application/json").Produces<TagDto>(201).Produces(400).RequireAuthorization();

            group.MapPut("/{id}", UpdateTag).WithName("UpdateTag")
                .Accepts<TagRequestDto>("application/json").Produces<TagDto>(200).Produces(400).Produces(404).RequireAuthorization();

            group.MapDelete("/{id}", DeleteTag).WithName("DeleteTag").Produces(204).Produces(404).RequireAuthorization();

        }

        private static async Task<IResult> GetAllTags(
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tags = await tagService.GetAllTags(userId);
            return Results.Ok(tags);
        }

        private static async Task<IResult> CreateTag(
            TagRequestDto tagDto,
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var (createdTag, errors, conflict) = await tagService.CreateTag(userId, tagDto);
            if (conflict)
                return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
            if (errors.Count > 0)
                return Results.BadRequest(errors);
            return Results.Created($"/api/tags/{createdTag.Id}", createdTag);
        }

        private static async Task<IResult> UpdateTag(
            Guid id,
            TagRequestDto tagDto,
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var (updatedTag, errors, notFound, conflict) = await tagService.UpdateTag(userId, id, tagDto);
            if (notFound)
                return Results.NotFound();
            if (conflict)
                return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
            if (errors.Count > 0)
                return Results.BadRequest(errors);
            return Results.Ok(updatedTag);
        }

        private static async Task<IResult> DeleteTag(
            Guid id,
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var deleted = await tagService.DeleteTag(userId, id);
            if (!deleted)
                return Results.NotFound();
            return Results.NoContent();
        }

        private static async Task<IResult> GetTagById(
            Guid id,
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tag = await tagService.GetTagById(userId, id);
            if (tag == null)
                return Results.NotFound();
            return Results.Ok(tag);
        }

        private static async Task<IResult> GetTagByName(
            string name,
            [FromServices] TagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tag = await tagService.GetTagByName(userId, name);
            if (tag == null)
                return Results.NotFound();
            return Results.Ok(tag);
        }
    }
} 