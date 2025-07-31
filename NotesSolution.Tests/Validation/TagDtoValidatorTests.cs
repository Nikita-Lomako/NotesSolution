using FluentValidation.TestHelper;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Validation;
using Xunit;

namespace NotesSolution.Tests.Validation
{
    public class TagDtoValidatorTests
    {
        private readonly TagDtoValidator _validator;

        public TagDtoValidatorTests()
        {
            _validator = new TagDtoValidator();
        }

        [Fact]
        public void Should_Pass_When_Valid_TagDto()
        {
            // Arrange
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
        [InlineData(null)]
        public void Should_Fail_When_Name_Is_Empty_Or_Null(string name)
        {
            // Arrange
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
                Name = name
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
                Name = "Tag With Spaces"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Id_Is_Empty_Guid()
        {
            // Arrange
            var dto = new TagDto
            {
                Id = Guid.Empty,
                Name = "Valid Tag"
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
                var dto = new TagDto
                {
                    Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
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
            var dto = new TagDto
            {
                Id = Guid.NewGuid(),
                Name = new string('x', 50)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }
    }
} 