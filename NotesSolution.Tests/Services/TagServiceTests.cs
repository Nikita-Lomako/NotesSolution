using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Application.Services;
using NotesSolution.Application.Dtos;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using Xunit;
using NotesSolution.Application.Interfaces;

namespace NotesSolution.Tests.Services
{
    public class TagServiceTests
    {
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<INoteRepository> _mockNoteRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<TagRequestDto>> _mockValidator;
        private readonly Mock<ILogger<TagService>> _mockLogger;
        private readonly ITagService _tagService;

        public TagServiceTests()
        {
            _mockTagRepository = new Mock<ITagRepository>();
            _mockNoteRepository = new Mock<INoteRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<TagRequestDto>>();
            _mockLogger = new Mock<ILogger<TagService>>();

            var mockCancellationTokenProvider = new Mock<ICancellationTokenProvider>();
            _tagService = new TagService(
                _mockTagRepository.Object,
                _mockNoteRepository.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                mockCancellationTokenProvider.Object
            );
        }

        [Fact]
        public async Task GetAllTags_ReturnsFilteredTagsForUser()
        {
            var userId = "user1";
            var tags = new List<Tag>
            {
                new Tag { Id = Guid.NewGuid(), Name = "Tag1", UserId = userId },
                new Tag { Id = Guid.NewGuid(), Name = "Tag2", UserId = userId }
            };
            var tagDtos = tags.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();
            _mockTagRepository.Setup(r => r.GetAllAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(tags);
            _mockMapper.Setup(m => m.Map<List<TagDto>>(tags)).Returns(tagDtos);
            var result = await _tagService.GetAllTags(userId, CancellationToken.None);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTagById_ReturnsTag_WhenTagExistsAndOwnedByUser()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = userId };
            var tagDto = new TagDto { Id = tagId, Name = "Test Tag" };
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
            _mockMapper.Setup(m => m.Map<TagDto>(tag)).Returns(tagDto);
            var result = await _tagService.GetTagById(userId, tagId, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(tagId, result.Id);
        }

        [Fact]
        public async Task GetTagById_ReturnsNull_WhenTagDoesNotExist()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);
            var result = await _tagService.GetTagById(userId, tagId, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagByName_ReturnsTag_WhenTagExistsAndOwnedByUser()
        {
            var userId = "user1";
            var tagName = "TestTag";
            var tag = new Tag { Id = Guid.NewGuid(), Name = tagName, UserId = userId };
            var tagDto = new TagDto { Id = tag.Id, Name = tagName };
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, tagName, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
            _mockMapper.Setup(m => m.Map<TagDto>(tag)).Returns(tagDto);
            var result = await _tagService.GetTagByName(userId, tagName, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(tagName, result.Name);
        }

        [Fact]
        public async Task CreateTag_ReturnsValidationErrors_WhenValidationFails()
        {
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "Test" };
            var validationErrors = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(validationErrors));
            var result = await _tagService.CreateTag(userId, tagDto, CancellationToken.None);
            var (tag, errors, conflict) = result;
            Assert.Null(tag);
            Assert.Single(errors);
            Assert.Contains("Name is required", errors);
            Assert.False(conflict);
        }

        [Fact]
        public async Task CreateTag_ReturnsConflict_WhenTagAlreadyExistsForUser()
        {
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "ExistingTag" };
            var existingTag = new Tag { Id = Guid.NewGuid(), Name = "ExistingTag", UserId = userId };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "ExistingTag", It.IsAny<CancellationToken>())).ReturnsAsync(existingTag);
            var result = await _tagService.CreateTag(userId, tagDto, CancellationToken.None);
            var (createdTag, errors, conflict) = result;
            Assert.Null(createdTag);
            Assert.Empty(errors);
            Assert.True(conflict);
        }

        [Fact]
        public async Task CreateTag_CreatesTagSuccessfully_WhenValidationPassesAndTagDoesNotExist()
        {
            var userId = "user1";
            var tagDto = new TagRequestDto { Name = "NewTag" };
            var tag = new Tag { Id = Guid.NewGuid(), Name = "NewTag", UserId = userId };
            var createdTagDto = new TagDto { Id = tag.Id, Name = "NewTag" };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "NewTag", It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);
            _mockTagRepository.Setup(r => r.CreateAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<TagDto>(It.IsAny<Tag>())).Returns(createdTagDto);
            var result = await _tagService.CreateTag(userId, tagDto, CancellationToken.None);
            var (createdTag, errors, conflict) = result;
            Assert.NotNull(createdTag);
            Assert.Empty(errors);
            Assert.False(conflict);
            _mockTagRepository.Verify(r => r.CreateAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTag_ReturnsNotFound_WhenTagDoesNotExist()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "UpdatedTag" };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);
            var result = await _tagService.UpdateTag(userId, tagId, tagDto, CancellationToken.None);
            var (updatedTag, errors, notFound, conflict) = result;
            Assert.Null(updatedTag);
            Assert.True(notFound);
            Assert.False(conflict);
        }

        [Fact]
        public async Task UpdateTag_ReturnsConflict_WhenTagWithSameNameExistsForUser()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "UpdatedTag" };
            var existingTag = new Tag { Id = tagId, Name = "OldTag", UserId = userId };
            var tagWithSameName = new Tag { Id = Guid.NewGuid(), Name = "UpdatedTag", UserId = userId };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTag);
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "UpdatedTag", It.IsAny<CancellationToken>())).ReturnsAsync(tagWithSameName);
            var result = await _tagService.UpdateTag(userId, tagId, tagDto, CancellationToken.None);
            var (updatedTag, errors, notFound, conflict) = result;
            Assert.Null(updatedTag);
            Assert.False(notFound);
            Assert.True(conflict);
        }

        [Fact]
        public async Task UpdateTag_UpdatesTagSuccessfully_WhenValidationPassesAndNoConflicts()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tagDto = new TagRequestDto { Name = "UpdatedTag" };
            var existingTag = new Tag { Id = tagId, Name = "OldTag", UserId = userId };
            _mockValidator.Setup(v => v.ValidateAsync(tagDto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTag);
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "UpdatedTag", It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);
            _mockTagRepository.Setup(r => r.UpdateAsync(existingTag, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<TagDto>(existingTag)).Returns(new TagDto { Id = tagId, Name = "UpdatedTag" });
            var result = await _tagService.UpdateTag(userId, tagId, tagDto, CancellationToken.None);
            var (updatedTag, errors, notFound, conflict) = result;
            Assert.NotNull(updatedTag);
            Assert.Empty(errors);
            Assert.False(notFound);
            Assert.False(conflict);
            _mockTagRepository.Verify(r => r.UpdateAsync(existingTag, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteTag_ReturnsTrue_WhenTagExistsAndOwnedByUser()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            var tag = new Tag { Id = tagId, Name = "Test Tag", UserId = userId };
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
            _mockTagRepository.Setup(r => r.RemoveAsync(tag, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockNoteRepository.Setup(r => r.GetAllAsync(userId, null, null, null, null, 1, int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Note>());
            var result = await _tagService.DeleteTag(userId, tagId, CancellationToken.None);
            Assert.True(result);
            _mockTagRepository.Verify(r => r.RemoveAsync(tag, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteTag_ReturnsFalse_WhenTagDoesNotExist()
        {
            var userId = "user1";
            var tagId = Guid.NewGuid();
            _mockTagRepository.Setup(r => r.GetAsync(userId, tagId, It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);
            var result = await _tagService.DeleteTag(userId, tagId, CancellationToken.None);
            Assert.False(result);
            _mockTagRepository.Verify(r => r.RemoveAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
} 