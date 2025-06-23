using FluentValidation;
using NotesSolution.Core.Dtos;

namespace NotesSolution.Core.Validation
{
    public class TagDtoValidator : AbstractValidator<TagDto>
    {
        public TagDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        }
    }
} 