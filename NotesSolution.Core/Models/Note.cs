using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesSolution.Core.Models
{
    public class Note : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Tag> Tags { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
    }
}
