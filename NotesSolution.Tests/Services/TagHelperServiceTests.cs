using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NotesSolution.Application.Services;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
using Xunit;
using System;

namespace NotesSolution.Tests.Services
{
    public class TagHelperServiceTests
    {
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly TagHelperService _service;

        public TagHelperServiceTests()
        {
            _mockTagRepository = new Mock<ITagRepository>();
            _service = new TagHelperService(_mockTagRepository.Object);
        }

        [Fact]
        public async Task GetOrCreateTagsAsync_CreatesNewTags_WhenNoneExist()
        {
            var userId = "user1";
            var tagNames = new List<string> { "tag1", "tag2" };
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "tag1")).ReturnsAsync((Tag?)null);
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "tag2")).ReturnsAsync((Tag?)null);
            _mockTagRepository.Setup(r => r.CreateAsync(It.IsAny<Tag>())).Returns(Task.CompletedTask);
            var result = await _service.GetOrCreateTagsAsync(tagNames, userId);
            Assert.Equal(2, result.Count);
            _mockTagRepository.Verify(r => r.CreateAsync(It.Is<Tag>(t => t.Name == "tag1" && t.UserId == userId)), Times.Once);
            _mockTagRepository.Verify(r => r.CreateAsync(It.Is<Tag>(t => t.Name == "tag2" && t.UserId == userId)), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateTagsAsync_ReturnsExistingTags_WhenTheyExist()
        {
            var userId = "user1";
            var tagNames = new List<string> { "tag1" };
            var existingTag = new Tag { Id = Guid.NewGuid(), Name = "tag1", UserId = userId };
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "tag1")).ReturnsAsync(existingTag);
            var result = await _service.GetOrCreateTagsAsync(tagNames, userId);
            Assert.Single(result);
            Assert.Equal(existingTag.Id, result[0].Id);
            _mockTagRepository.Verify(r => r.CreateAsync(It.IsAny<Tag>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateTagsAsync_MixedScenario_CreatesAndReturnsTags()
        {
            var userId = "user1";
            var tagNames = new List<string> { "tag1", "tag2" };
            var existingTag = new Tag { Id = Guid.NewGuid(), Name = "tag1", UserId = userId };
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "tag1")).ReturnsAsync(existingTag);
            _mockTagRepository.Setup(r => r.GetByNameAsync(userId, "tag2")).ReturnsAsync((Tag?)null);
            _mockTagRepository.Setup(r => r.CreateAsync(It.IsAny<Tag>())).Returns(Task.CompletedTask);
            var result = await _service.GetOrCreateTagsAsync(tagNames, userId);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Name == "tag1" && t.Id == existingTag.Id);
            Assert.Contains(result, t => t.Name == "tag2");
            _mockTagRepository.Verify(r => r.CreateAsync(It.Is<Tag>(t => t.Name == "tag2" && t.UserId == userId)), Times.Once);
        }
    }
} 