using FluentValidation;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Validation
{
    public class NoteCreateDtoValidator : AbstractValidator<NoteCreateDto>
    {
        public NoteCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Tags).NotNull();
            RuleFor(x => x.ImageUrls).NotNull();
        }
    }
} 