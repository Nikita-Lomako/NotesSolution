using FluentValidation;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Validation
{
    public class TagDtoValidator : AbstractValidator<TagDto>
    {
        public TagDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        }
    }
} 