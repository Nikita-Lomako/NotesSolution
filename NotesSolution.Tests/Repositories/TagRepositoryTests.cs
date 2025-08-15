using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Core.Models;
using NotesSolution.Infrastructure.Data;
using NotesSolution.Infrastructure.Repositories;
using Xunit;

namespace NotesSolution.Tests.Repositories
{
    public class TagRepositoryTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateAndGet_Tag_Success()
        {
            var db = GetDbContext(nameof(CreateAndGet_Tag_Success));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            var tag = new Tag { Name = "Test", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            var fetched = await repo.GetByNameAsync("user1", "Test");
            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched.Name);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyUserTags()
        {
            var db = GetDbContext(nameof(GetAllAsync_ReturnsOnlyUserTags));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            await repo.CreateAsync(new Tag { Name = "Tag1", UserId = "user1" });
            await repo.CreateAsync(new Tag { Name = "Tag2", UserId = "user2" });
            await repo.SaveAsync();
            var user1Tags = await repo.GetAllAsync("user1");
            var user2Tags = await repo.GetAllAsync("user2");
            Assert.Single(user1Tags);
            Assert.Single(user2Tags);
            Assert.Equal("user1", user1Tags.First().UserId);
            Assert.Equal("user2", user2Tags.First().UserId);
        }

        [Fact]
        public async Task GetAsync_ReturnsNullForOtherUser()
        {
            var db = GetDbContext(nameof(GetAsync_ReturnsNullForOtherUser));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            var tag = new Tag { Name = "Tag", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            var fetched = await repo.GetAsync("user2", tag.Id);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNullForOtherUser()
        {
            var db = GetDbContext(nameof(GetByNameAsync_ReturnsNullForOtherUser));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            var tag = new Tag { Name = "Tag", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            var fetched = await repo.GetByNameAsync("user2", "Tag");
            Assert.Null(fetched);
        }

        [Fact]
        public async Task UpdateAsync_ChangesTagName()
        {
            var db = GetDbContext(nameof(UpdateAsync_ChangesTagName));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            var tag = new Tag { Name = "Old", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            tag.Name = "New";
            await repo.UpdateAsync(tag);
            await repo.SaveAsync();
            var updated = await repo.GetByNameAsync("user1", "New");
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Name);
        }

        [Fact]
        public async Task RemoveAsync_DeletesTag()
        {
            var db = GetDbContext(nameof(RemoveAsync_DeletesTag));
            var logger = Mock.Of<ILogger<TagRepository>>();
            var repo = new TagRepository(db, logger);
            var tag = new Tag { Name = "ToDelete", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            await repo.RemoveAsync(tag);
            await repo.SaveAsync();
            var deleted = await repo.GetByNameAsync("user1", "ToDelete");
            Assert.Null(deleted);
        }
    }
}
