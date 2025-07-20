using FluentValidation;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Validation
{
    public class TagRequestDtoValidator : AbstractValidator<TagRequestDto>
    {
        public TagRequestDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        }
    }
} 