using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.API.Services;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using Xunit;

namespace NotesSolution.Tests.Services
{
    public class TagServiceTests
    {
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<INoteRepository> _mockNoteRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<TagRequestDto>> _mockValidator;
        private readonly Mock<ILogger<TagService>> _mockLogger;
        private readonly TagService _tagService;

        public TagServiceTests()
        {
            _mockTagRepository = new Mock<ITagRepository>();
            _mockNoteRepository = new Mock<INoteRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<TagRequestDto>>();
            _mockLogger = new Mock<ILogger<TagService>>();

            _tagService = new TagService(
                _mockTagRepository.Object,
                _mockNoteRepository.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetAllTags_ReturnsFilteredTagsForUser()
        {
            // Arrange
            var userId = "user1";
            var tags = new List<Tag>
            {
                new Tag { Id = Guid.NewGuid(), Name = "Tag1", UserId = userId },
                new Tag { Id = Guid.NewGuid(), Name = "Tag2", UserId = userId },
                new Tag { Id = Guid.NewGuid(), Name = "Tag3", UserId = "user2" }
            };
            var tagDtos = tags.Where(t => t.UserId == userId).Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();

            _mockTagRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(tags);
            _mockMapper.Setup(m => m.Map<List<TagDto>>(It.IsAny<List<Tag>>()))
                .Returns(tagDtos);

            // Act
            var result = await _tagService.GetAllTags(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, dto => Assert.Equal(userId, tags.First(t => t.Id == dto.Id).UserId));
        }

        [Fact]
        public async Task GetTagById_ReturnsTag_WhenTagExistsAndOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = userId };
            var tagDto = new TagDto { Id = tagId, Name = "Test Tag" };

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(tag);
            _mockMapper.Setup(m => m.Map<TagDto>(tag))
                .Returns(tagDto);

            // Act
            var result = await _tagService.GetTagById(userId, tagId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tagId, result.Id);
        }

        [Fact]
        public async Task GetTagById_ReturnsNull_WhenTagDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync((Tag?)null);

            // Act
            var result = await _tagService.GetTagById(userId, tagId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagById_ReturnsNull_WhenTagNotOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = "user2" };

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(tag);

            // Act
            var result = await _tagService.GetTagById(userId, tagId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagByName_ReturnsTag_WhenTagExistsAndOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var tagName = "TestTag";
            var tag = new Tag { Id = Guid.NewGuid(), Name = tagName, UserId = userId };
            var tagDto = new TagDto { Id = tag.Id, Name = tagName };

            _mockTagRepository.Setup(r => r.GetByNameAsync(tagName))
                .ReturnsAsync(tag);
            _mockMapper.Setup(m => m.Map<TagDto>(tag))
                .Returns(tagDto);

            // Act
            var result = await _tagService.GetTagByName(userId, tagName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tagName, result.Name);
        }

        [Fact]
        public async Task CreateTag_ReturnsValidationErrors_WhenValidationFails()
        {
            // Arrange
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "Test" };
            var validationErrors = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult(validationErrors));

            // Act
            var result = await _tagService.CreateTag(userId, tagDto);
            var (tag, errors, conflict) = result;

            // Assert
            Assert.Null(tag);
            Assert.Single(errors);
            Assert.Contains("Name is required", errors);
            Assert.False(conflict);
        }

        [Fact]
        public async Task CreateTag_ReturnsConflict_WhenTagAlreadyExistsForUser()
        {
            // Arrange
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "ExistingTag" };
            var existingTag = new Tag { Id = Guid.NewGuid(), Name = "ExistingTag", UserId = userId };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetByNameAsync("ExistingTag"))
                .ReturnsAsync(existingTag);

            // Act
            var result = await _tagService.CreateTag(userId, tagDto);
            var (createdTag, errors, conflict) = result;

            // Assert
            Assert.Null(createdTag);
            Assert.Empty(errors);
            Assert.True(conflict);
        }

        [Fact]
        public async Task CreateTag_CreatesTagSuccessfully_WhenValidationPassesAndTagDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "NewTag" };
            var tag = new Tag { Id = Guid.NewGuid(), Name = "NewTag", UserId = userId };
            var createdTagDto = new TagDto { Id = tag.Id, Name = "NewTag" };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetByNameAsync("NewTag"))
                .ReturnsAsync((Tag?)null);
            _mockMapper.Setup(m => m.Map<TagDto>(It.IsAny<Tag>()))
                .Returns(createdTagDto);

            // Act
            var result = await _tagService.CreateTag(userId, tagDto);
            var (createdTag, errors, conflict) = result;

            // Assert
            Assert.NotNull(createdTag);
            Assert.Empty(errors);
            Assert.False(conflict);
            _mockTagRepository.Verify(r => r.CreateAsync(It.IsAny<Tag>()), Times.Once);
            _mockTagRepository.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTag_ReturnsNotFound_WhenTagDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "UpdatedTag" };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync((Tag?)null);

            // Act
            var result = await _tagService.UpdateTag(userId, tagId, tagDto);
            var (updatedTag, errors, notFound, conflict) = result;

            // Assert
            Assert.Null(updatedTag);
            Assert.True(notFound);
            Assert.False(conflict);
        }

        [Fact]
        public async Task UpdateTag_ReturnsConflict_WhenTagWithSameNameExistsForUser()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "ExistingTag" };
            var existingTag = new Tag { Id = tagId, Name = "OldName", UserId = userId };
            var conflictingTag = new Tag { Id = Guid.NewGuid(), Name = "ExistingTag", UserId = userId };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(existingTag);
            _mockTagRepository.Setup(r => r.GetByNameAsync("ExistingTag"))
                .ReturnsAsync(conflictingTag);

            // Act
            var result = await _tagService.UpdateTag(userId, tagId, tagDto);
            var (updatedTag, errors, notFound, conflict) = result;

            // Assert
            Assert.Null(updatedTag);
            Assert.False(notFound);
            Assert.True(conflict);
        }

        [Fact]
        public async Task UpdateTag_UpdatesTagSuccessfully_WhenValidationPassesAndNoConflicts()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "UpdatedTag" };
            var existingTag = new Tag { Id = tagId, Name = "OldName", UserId = userId };
            var updatedTagDto = new TagDto { Id = tagId, Name = "UpdatedTag" };

            _mockValidator.Setup(v => v.ValidateAsync(tagDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(existingTag);
            _mockTagRepository.Setup(r => r.GetByNameAsync("UpdatedTag"))
                .ReturnsAsync((Tag?)null);
            _mockMapper.Setup(m => m.Map<TagDto>(It.IsAny<Tag>()))
                .Returns(updatedTagDto);

            // Act
            var result = await _tagService.UpdateTag(userId, tagId, tagDto);
            var (updatedTag, errors, notFound, conflict) = result;

            // Assert
            Assert.NotNull(updatedTag);
            Assert.Empty(errors);
            Assert.False(notFound);
            Assert.False(conflict);
            _mockTagRepository.Verify(r => r.UpdateAsync(It.IsAny<Tag>()), Times.Once);
            _mockTagRepository.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteTag_ReturnsTrue_WhenTagExistsAndOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = userId };
            var notes = new List<Note>
            {
                new Note { Id = Guid.NewGuid(), Name = "Note1", UserId = userId, Tags = new List<Tag> { tag } }
            };

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(tag);
            _mockNoteRepository.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(notes);

            // Act
            var result = await _tagService.DeleteTag(userId, tagId);

            // Assert
            Assert.True(result);
            _mockTagRepository.Verify(r => r.RemoveAsync(tag), Times.Once);
            _mockTagRepository.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteTag_ReturnsFalse_WhenTagDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync((Tag?)null);

            // Act
            var result = await _tagService.DeleteTag(userId, tagId);

            // Assert
            Assert.False(result);
            _mockTagRepository.Verify(r => r.RemoveAsync(It.IsAny<Tag>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTag_ReturnsFalse_WhenTagNotOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = "user2" };

            _mockTagRepository.Setup(r => r.GetAsync(tagId))
                .ReturnsAsync(tag);

            // Act
            var result = await _tagService.DeleteTag(userId, tagId);

            // Assert
            Assert.False(result);
            _mockTagRepository.Verify(r => r.RemoveAsync(It.IsAny<Tag>()), Times.Never);
        }
    }
} 