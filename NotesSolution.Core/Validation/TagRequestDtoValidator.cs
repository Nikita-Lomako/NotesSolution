using FluentValidation;
using NotesSolution.Core.Dtos;

namespace NotesSolution.Core.Validation
{
    public class TagRequestDtoValidator : AbstractValidator<TagRequestDto>
    {
        public TagRequestDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        }
    }
} 