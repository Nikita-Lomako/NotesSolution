using FluentValidation;
using NotesSolution.Core.Dtos;

namespace NotesSolution.Core.Validation
{
    public class NoteUpdateDtoValidator : AbstractValidator<NoteUpdateDto>
    {
        public NoteUpdateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Tags).NotNull();
            RuleFor(x => x.ImageUrls).NotNull();
        }
    }
} 