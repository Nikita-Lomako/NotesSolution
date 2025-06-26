using NotesSolution.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotesSolution.Core.Interfaces
{
    public interface ITagHelperService
    {
        Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, string userId);
    }
} 