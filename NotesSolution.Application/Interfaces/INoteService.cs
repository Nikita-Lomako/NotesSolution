using Microsoft.AspNetCore.Http;
using NotesSolution.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotesSolution.Application.Services
{
    public interface INoteService
    {
        Task<List<NoteDto>> GetAllNotes(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<NoteDto?> GetNoteById(string userId, Guid id, CancellationToken cancellationToken = default);
        Task<(NoteDto? note, List<string> errors)> CreateNote(string userId, NoteCreateDto noteDto, IFormFileCollection? images, CancellationToken cancellationToken = default);
        Task<(NoteDto? note, List<string> errors, bool notFound)> UpdateNote(string userId, Guid id, NoteUpdateDto noteDto, IFormFileCollection? images, CancellationToken cancellationToken = default);
        Task<bool> DeleteNote(string userId, Guid id, CancellationToken cancellationToken = default);
    }
}
