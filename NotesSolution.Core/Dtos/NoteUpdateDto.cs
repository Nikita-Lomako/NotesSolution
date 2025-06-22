namespace NotesSolution.Core.Dtos
{
    public class NoteUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
    }
} 