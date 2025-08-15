namespace NotesSolution.Application.Dtos
{
    public class NoteDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public DateTime CreationDate { get; set; }
    }
}
