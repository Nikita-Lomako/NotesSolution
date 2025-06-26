using AutoMapper;
using Microsoft.AspNetCore.Identity;
using NotesSolution.Core;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Models;
using Xunit;

namespace NotesSolution.Tests.Utils
{
    public class MappingConfigTests
    {
        private readonly IMapper _mapper;

        public MappingConfigTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingConfig>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void NoteToNoteDto_ShouldMapCorrectly()
        {
            // Arrange
            var note = new Note
            {
                Id = Guid.NewGuid(),
                Name = "Test Note",
                Description = "Test Description",
                UserId = "user123",
                CreationDate = DateTime.UtcNow,
                Tags = new List<Tag>
                {
                    new Tag { Id = Guid.NewGuid(), Name = "Tag1", UserId = "user123" },
                    new Tag { Id = Guid.NewGuid(), Name = "Tag2", UserId = "user123" }
                },
                ImageUrls = new List<string> { "image1.jpg", "image2.jpg" }
            };

            // Act
            var noteDto = _mapper.Map<NoteDto>(note);

            // Assert
            Assert.Equal(note.Id, noteDto.Id);
            Assert.Equal(note.Name, noteDto.Name);
            Assert.Equal(note.Description, noteDto.Description);
            Assert.Equal(note.CreationDate, noteDto.CreationDate);
            Assert.Equal(note.Tags.Count, noteDto.Tags.Count);
            Assert.Equal(note.ImageUrls.Count, noteDto.ImageUrls.Count);
        }
        

        [Fact]
        public void TagToTagDto_ShouldMapCorrectly()
        {
            // Arrange
            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                Name = "Test Tag",
                UserId = "user123"
            };

            // Act
            var tagDto = _mapper.Map<TagDto>(tag);

            // Assert
            Assert.Equal(tag.Id, tagDto.Id);
            Assert.Equal(tag.Name, tagDto.Name);
        }

        [Fact]
        public void TagDtoToTag_ShouldMapCorrectly()
        {
            // Arrange
            var tagDto = new TagDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Tag"
            };

            // Act
            var tag = _mapper.Map<Tag>(tagDto);

            // Assert
            Assert.Equal(tagDto.Id, tag.Id);
            Assert.Equal(tagDto.Name, tag.Name);
        }
        
        [Fact]
        public void IdentityUser_To_UserDto_Mapping_Should_Work_Correctly()
        {
            // Arrange
            var identityUser = new IdentityUser
            {
                Id = "user123",
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true,
                PhoneNumber = "1234567890",
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnd = null,
                LockoutEnabled = false,
                AccessFailedCount = 0
            };

            // Act
            var userDto = _mapper.Map<UserDto>(identityUser);

            // Assert
            Assert.Equal(identityUser.Id, userDto.Id);
            Assert.Equal(identityUser.UserName, userDto.Name);
        }      
    }
}
