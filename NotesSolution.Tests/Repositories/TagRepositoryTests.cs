using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var repo = new TagRepository(db);
            var tag = new Tag { Name = "Test", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            var fetched = await repo.GetByNameAsync("Test");
            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched.Name);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyUserTags()
        {
            var db = GetDbContext(nameof(GetAllAsync_ReturnsOnlyUserTags));
            var repo = new TagRepository(db);
            await repo.CreateAsync(new Tag { Name = "Tag1", UserId = "user1" });
            await repo.CreateAsync(new Tag { Name = "Tag2", UserId = "user2" });
            await repo.SaveAsync();
            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task UpdateAsync_ChangesTagName()
        {
            var db = GetDbContext(nameof(UpdateAsync_ChangesTagName));
            var repo = new TagRepository(db);
            var tag = new Tag { Name = "Old", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            tag.Name = "New";
            await repo.UpdateAsync(tag);
            await repo.SaveAsync();
            var updated = await repo.GetByNameAsync("New");
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Name);
        }

        [Fact]
        public async Task RemoveAsync_DeletesTag()
        {
            var db = GetDbContext(nameof(RemoveAsync_DeletesTag));
            var repo = new TagRepository(db);
            var tag = new Tag { Name = "ToDelete", UserId = "user1" };
            await repo.CreateAsync(tag);
            await repo.SaveAsync();
            await repo.RemoveAsync(tag);
            await repo.SaveAsync();
            var deleted = await repo.GetByNameAsync("ToDelete");
            Assert.Null(deleted);
        }
    }
}
