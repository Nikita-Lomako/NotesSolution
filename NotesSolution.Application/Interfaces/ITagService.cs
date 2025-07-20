using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Interfaces
{
    public interface ITagService
    {
        Task<List<TagDto>> GetAllTags(string userId);
        Task<TagDto?> GetTagById(string userId, Guid id);
        Task<TagDto?> GetTagByName(string userId, string name);
        Task<(TagDto? tag, List<string> errors, bool conflict)> CreateTag(string userId, TagRequestDto tagDto);
        Task<(TagDto? tag, List<string> errors, bool notFound, bool conflict)> UpdateTag(string userId, Guid id, TagRequestDto tagDto);
        Task<bool> DeleteTag(string userId, Guid id);
    }
}  