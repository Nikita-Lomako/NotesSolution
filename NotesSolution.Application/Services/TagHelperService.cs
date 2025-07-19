using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotesSolution.Application.Services
{
    public class TagHelperService : ITagHelperService
    {
        private readonly ITagRepository _tagRepository;
        public TagHelperService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, string userId)
        {
            var tagEntities = new List<Tag>();
            foreach (var tagName in tagNames)
            {
                var existingTag = await _tagRepository.GetByNameAsync(tagName);
                if (existingTag != null && existingTag.UserId == userId)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName, UserId = userId };
                    await _tagRepository.CreateAsync(newTag);
                    tagEntities.Add(newTag);
                }
            }
            return tagEntities;
        }
    }
} 