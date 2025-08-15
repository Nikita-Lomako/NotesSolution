using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotesSolution.Core.Models;

namespace NotesSolution.Core.Interfaces
{
    public interface ITagHelperService
    {
        Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, string userId, CancellationToken cancellationToken = default);
    }
}
