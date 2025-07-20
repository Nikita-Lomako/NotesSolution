using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using NotesSolution.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Services;

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
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var notes = await noteService.GetAllNotes(userId, search, tag, sort, order, page, pageSize);
            return Results.Ok(notes);
        }

        private static async Task<IResult> GetNoteById(
            Guid id,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var note = await noteService.GetNoteById(userId, id);
            if (note == null)
                return Results.NotFound();
            return Results.Ok(note);
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
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var normalizedTags = NormalizeTags(tags);
            var noteDto = new NoteCreateDto
            {
                Name = name,
                Description = description,
                Tags = normalizedTags,
                ImageUrls = new List<string>()
            };
            var (createdNote, errors) = await noteService.CreateNote(userId, noteDto, images);
            if (errors.Count > 0)
                return Results.BadRequest(errors);
            return Results.Created($"/api/notes/{createdNote?.Id}", createdNote);
        }

        private static async Task<IResult> UpdateNote(
            Guid id,
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string tags,
            [FromForm] IFormFileCollection? images,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var normalizedTags = NormalizeTags(tags);
            var noteDto = new NoteUpdateDto
            {
                Name = name,
                Description = description,
                Tags = normalizedTags,
                ImageUrls = new List<string>()
            };
            var (updatedNote, errors, notFound) = await noteService.UpdateNote(userId, id, noteDto, images);
            if (notFound)
                return Results.NotFound();
            if (errors.Count > 0)
                return Results.BadRequest(errors);
            return Results.Ok(updatedNote);
        }

        private static async Task<IResult> DeleteNote(
            Guid id,
            [FromServices] INoteService noteService,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var deleted = await noteService.DeleteNote(userId, id);
            if (!deleted)
                return Results.NotFound();
            return Results.NoContent();
        }
    }
}