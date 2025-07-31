using FluentValidation.TestHelper;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Validation;
using Xunit;

namespace NotesSolution.Tests.Validation
{
    public class NoteCreateDtoValidatorTests
    {
        private readonly NoteCreateDtoValidator _validator;

        public NoteCreateDtoValidatorTests()
        {
            _validator = new NoteCreateDtoValidator();
        }

        [Fact]
        public void Should_Pass_When_Valid_NoteCreateDto()
        {
            // Arrange
            var dto = new NoteCreateDto
            {
                Name = "Test Note",
                Description = "Test Description",
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
        public void Should_Fail_When_Name_Is_Empty_Or_Null(string name)
        {
            // Arrange
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
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
            var dto = new NoteCreateDto
            {
                Name = "My Important Note",
                Description = "This is a very detailed description with special characters: !@#$%^&*() and numbers 1234567890",
                Tags = new List<string> { "important", "work", "project", "deadline" },
                ImageUrls = new List<string> 
                { 
                    "https://example.com/image1.jpg",
                    "https://example.com/image2.png",
                    "https://example.com/image3.gif"
                }
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
} 