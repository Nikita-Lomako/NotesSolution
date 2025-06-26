using FluentValidation.TestHelper;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Validation;
using Xunit;

namespace NotesSolution.Tests.Validation
{
    public class NoteUpdateDtoValidatorTests
    {
        private readonly NoteUpdateDtoValidator _validator;

        public NoteUpdateDtoValidatorTests()
        {
            _validator = new NoteUpdateDtoValidator();
        }

        [Fact]
        public void Should_Pass_When_Valid_NoteUpdateDto()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Updated Note",
                Description = "Updated Description",
                Tags = new List<string> { "tag1", "tag2" },
                ImageUrls = new List<string> { "image1.jpg", "image2.jpg" }
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
        public void Should_Fail_When_Name_Is_Empty_Or_Null(string? name)
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = name,
                Description = "Valid description",
                Tags = new List<string> { "tag1" },
                ImageUrls = new List<string> { "image1.jpg" }
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
            var dto = new NoteUpdateDto
            {
                Name = new string('a', 101), // 101 characters
                Description = "Test Description",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
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
            var dto = new NoteUpdateDto
            {
                Name = new string('a', 100), // Exactly 100 characters
                Description = "Test Description",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Pass_When_Description_Is_Empty()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = "",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_Pass_When_Description_Is_Exactly_Maximum_Length()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = new string('a', 1000), // Exactly 1000 characters
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_Fail_When_Description_Exceeds_Maximum_Length()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = new string('a', 1001), // 1001 characters
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_Fail_When_Tags_Is_Null()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = "Test Description",
                Tags = null!,
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Tags);
        }

        [Fact]
        public void Should_Pass_When_Tags_Is_Empty_List()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = "Test Description",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Tags);
        }

        [Fact]
        public void Should_Fail_When_ImageUrls_Is_Null()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = "Test Description",
                Tags = new List<string>(),
                ImageUrls = null!
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImageUrls);
        }

        [Fact]
        public void Should_Pass_When_ImageUrls_Is_Empty_List()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Test Note",
                Description = "Test Description",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ImageUrls);
        }

        [Fact]
        public void Should_Pass_When_All_Properties_Are_Valid_With_Complex_Data()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "My Updated Important Note",
                Description = "This is an updated detailed description with special characters: !@#$%^&*() and numbers 1234567890",
                Tags = new List<string> { "important", "work", "project", "deadline", "updated" },
                ImageUrls = new List<string> 
                { 
                    "https://example.com/updated-image1.jpg",
                    "https://example.com/updated-image2.png",
                    "https://example.com/updated-image3.gif"
                }
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Pass_When_Minimal_Valid_Data()
        {
            // Arrange
            var dto = new NoteUpdateDto
            {
                Name = "Minimal Note",
                Description = "",
                Tags = new List<string>(),
                ImageUrls = new List<string>()
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
} 