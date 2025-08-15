using System.Net;
using System.Security.Claims;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Services;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;

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
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromQuery] string? search, [FromQuery] string? tag, [FromQuery] string? sort, [FromQuery] string? order,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var notes = await noteService.GetAllNotes(userId, search, tag, sort, order, page, pageSize, cancellationToken);
                return Results.Ok(notes);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while retrieving notes", statusCode: 500);
            }
        }

        private static async Task<IResult> GetNoteById(
            Guid id,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var note = await noteService.GetNoteById(userId, id, cancellationToken);
                if (note == null)
                    return Results.NotFound();
                return Results.Ok(note);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while retrieving the note", statusCode: 500);
            }
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
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var normalizedTags = NormalizeTags(tags);
                var noteDto = new NoteCreateDto
                {
                    Name = name,
                    Description = description,
                    Tags = normalizedTags,
                    ImageUrls = new List<string>()
                };
                var (createdNote, errors) = await noteService.CreateNote(userId, noteDto, images, cancellationToken);
                if (errors.Count > 0)
                    return Results.BadRequest(errors);
                return Results.Created($"/api/notes/{createdNote?.Id}", createdNote);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while creating the note", statusCode: 500);
            }
        }

        private static async Task<IResult> UpdateNote(
            Guid id,
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string tags,
            [FromForm] IFormFileCollection? images,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var normalizedTags = NormalizeTags(tags);
                var noteDto = new NoteUpdateDto
                {
                    Name = name,
                    Description = description,
                    Tags = normalizedTags,
                    ImageUrls = new List<string>()
                };
                var (updatedNote, errors, notFound) = await noteService.UpdateNote(userId, id, noteDto, images, cancellationToken);
                if (notFound)
                    return Results.NotFound();
                if (errors.Count > 0)
                    return Results.BadRequest(errors);
                return Results.Ok(updatedNote);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while updating the note", statusCode: 500);
            }
        }

        private static async Task<IResult> DeleteNote(
            Guid id,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Results.Unauthorized();
                var deleted = await noteService.DeleteNote(userId, id, cancellationToken);
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
                return Results.Problem("An error occurred while deleting the note", statusCode: 500);
            }
        }
    }
}
