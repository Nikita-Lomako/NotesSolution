using System.Security.Claims;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;

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
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var tags = await tagService.GetAllTags(userId, cancellationToken);
                return Results.Ok(tags);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while retrieving tags", statusCode: 500);
            }
        }

        private static async Task<IResult> CreateTag(
            TagRequestDto tagDto,
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var (createdTag, errors, conflict) = await tagService.CreateTag(userId, tagDto, cancellationToken);
                if (conflict)
                    return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
                if (errors.Count > 0)
                    return Results.BadRequest(errors);
                return Results.Created($"/api/tags/{createdTag?.Id}", createdTag);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while creating the tag", statusCode: 500);
            }
        }

        private static async Task<IResult> UpdateTag(
            Guid id,
            TagRequestDto tagDto,
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var (updatedTag, errors, notFound, conflict) = await tagService.UpdateTag(userId, id, tagDto, cancellationToken);
                if (notFound)
                    return Results.NotFound();
                if (conflict)
                    return Results.Conflict($"Tag with name '{tagDto.Name}' already exists.");
                if (errors.Count > 0)
                    return Results.BadRequest(errors);
                return Results.Ok(updatedTag);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while updating the tag", statusCode: 500);
            }
        }

        private static async Task<IResult> DeleteTag(
            Guid id,
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var deleted = await tagService.DeleteTag(userId, id, cancellationToken);
                if (!deleted)
                    return Results.NotFound();
                return Results.NoContent();
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while deleting the tag", statusCode: 500);
            }
        }

        private static async Task<IResult> GetTagById(
            Guid id,
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var tag = await tagService.GetTagById(userId, id, cancellationToken);
                if (tag == null)
                    return Results.NotFound();
                return Results.Ok(tag);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while retrieving the tag", statusCode: 500);
            }
        }

        private static async Task<IResult> GetTagByName(
            string name,
            [FromServices] ITagService tagService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var tag = await tagService.GetTagByName(userId, name, cancellationToken);
                if (tag == null)
                    return Results.NotFound();
                return Results.Ok(tag);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while retrieving the tag", statusCode: 500);
            }
        }
    }
}
