using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Interfaces
{
    public interface ITagService
    {
        Task<List<TagDto>> GetAllTags(string userId, CancellationToken cancellationToken = default);
        Task<TagDto?> GetTagById(string userId, Guid id, CancellationToken cancellationToken = default);
        Task<TagDto?> GetTagByName(string userId, string name, CancellationToken cancellationToken = default);
        Task<(TagDto? tag, List<string> errors, bool conflict)> CreateTag(string userId, TagRequestDto tagDto, CancellationToken cancellationToken = default);
        Task<(TagDto? tag, List<string> errors, bool notFound, bool conflict)> UpdateTag(string userId, Guid id, TagRequestDto tagDto, CancellationToken cancellationToken = default);
        Task<bool> DeleteTag(string userId, Guid id, CancellationToken cancellationToken = default);
    }
}
