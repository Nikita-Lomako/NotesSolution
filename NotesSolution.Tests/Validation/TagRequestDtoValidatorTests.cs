using FluentValidation.TestHelper;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Validation;
using Xunit;

namespace NotesSolution.Tests.Validation
{
    public class TagRequestDtoValidatorTests
    {
        private readonly TagRequestDtoValidator _validator;

        public TagRequestDtoValidatorTests()
        {
            _validator = new TagRequestDtoValidator();
        }

        [Fact]
        public void Should_Pass_When_Valid_TagRequestDto()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "Test Tag"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Fail_When_Name_Is_Empty_Or_Null(string name)
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = name
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Fail_When_Name_Is_Null()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = null!
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Fail_When_Name_Exceeds_Maximum_Length()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = new string('a', 51) // 51 characters
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Is_Exactly_Maximum_Length()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = new string('a', 50) // Exactly 50 characters
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Is_Single_Character()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "A"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Special_Characters()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "Tag-With_Special@Characters#123"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Unicode_Characters()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "Тег-с-кириллицей"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Numbers()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "Tag123"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Spaces()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "Tag With Spaces"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Is_Common_Tag_Names()
        {
            // Arrange
            var commonTagNames = new[]
            {
                "work",
                "personal",
                "important",
                "urgent",
                "project",
                "meeting",
                "todo",
                "done",
                "in-progress",
                "blocked"
            };

            foreach (var tagName in commonTagNames)
            {
                var dto = new TagRequestDto
                {
                    Name = tagName
                };

                // Act
                var result = _validator.TestValidate(dto);

                // Assert
                result.ShouldNotHaveValidationErrorFor(x => x.Name);
            }
        }

        [Fact]
        public void Should_Pass_When_Name_Is_Exactly_One_Character()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "X"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Is_Exactly_Fifty_Characters()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = new string('x', 50)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Mixed_Case()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "MixedCaseTag"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Underscores()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "tag_with_underscores"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Hyphens()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "tag-with-hyphens"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Dots()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "tag.with.dots"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_At_Symbol()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "tag@domain"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Hash_Symbol()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "tag#123"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Exclamation_Mark()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "important!"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Name_Contains_Question_Mark()
        {
            // Arrange
            var dto = new TagRequestDto
            {
                Name = "urgent?"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }
    }
}
