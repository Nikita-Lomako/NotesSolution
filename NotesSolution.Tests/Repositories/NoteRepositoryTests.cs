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
    public class NoteRepositoryTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateAndGet_Note_Success()
        {
            var db = GetDbContext(nameof(CreateAndGet_Note_Success));
            var repo = new NoteRepository(db);
            var note = new Note { Name = "TestNote", Description = "desc", UserId = "user1" };
            await repo.CreateAsync(note);
            await repo.SaveAsync();
            var fetched = await repo.GetAsync("user1", note.Id);
            Assert.NotNull(fetched);
            Assert.Equal("TestNote", fetched.Name);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByUserId_AndPaginates()
        {
            var db = GetDbContext(nameof(GetAllAsync_FiltersByUserId_AndPaginates));
            var repo = new NoteRepository(db);
            for (int i = 1; i <= 10; i++)
                await repo.CreateAsync(new Note { Name = $"Note{i}", Description = "desc", UserId = "user1" });
            for (int i = 1; i <= 5; i++)
                await repo.CreateAsync(new Note { Name = $"Other{i}", Description = "desc", UserId = "user2" });
            await repo.SaveAsync();
            var notes = await repo.GetAllAsync("user1", null, null, null, null, 1, 10);
            Assert.Equal(10, notes.Count);
            Assert.All(notes, n => Assert.Equal("user1", n.UserId));
            var paged = await repo.GetAllAsync("user1", null, null, null, null, 2, 5);
            Assert.Equal(5, paged.Count);
        }

        [Fact]
        public async Task GetAllAsync_SearchAndTagFilterAndSort()
        {
            var db = GetDbContext(nameof(GetAllAsync_SearchAndTagFilterAndSort));
            var repo = new NoteRepository(db);
            var tag = new Tag { Name = "tag1", UserId = "user1" };
            await db.Tags.AddAsync(tag);
            await repo.CreateAsync(new Note { Name = "Alpha", Description = "desc", UserId = "user1", Tags = new List<Tag> { tag } });
            await repo.CreateAsync(new Note { Name = "Beta", Description = "desc", UserId = "user1" });
            await repo.SaveAsync();
            var search = await repo.GetAllAsync("user1", "Alpha", null, null, null, 1, 10);
            Assert.Single(search);
            var tagFilter = await repo.GetAllAsync("user1", null, "tag1", null, null, 1, 10);
            Assert.Single(tagFilter);
            var sorted = await repo.GetAllAsync("user1", null, null, "name", "desc", 1, 10);
            Assert.Equal("Beta", sorted.First().Name);
        }

        [Fact]
        public async Task UpdateAsync_ChangesNoteName()
        {
            var db = GetDbContext(nameof(UpdateAsync_ChangesNoteName));
            var repo = new NoteRepository(db);
            var note = new Note { Name = "Old", Description = "desc", UserId = "user1" };
            await repo.CreateAsync(note);
            await repo.SaveAsync();
            note.Name = "New";
            await repo.UpdateAsync(note);
            await repo.SaveAsync();
            var updated = await repo.GetAsync("user1", note.Id);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Name);
        }

        [Fact]
        public async Task RemoveAsync_DeletesNote()
        {
            var db = GetDbContext(nameof(RemoveAsync_DeletesNote));
            var repo = new NoteRepository(db);
            var note = new Note { Name = "ToDelete", Description = "desc", UserId = "user1" };
            await repo.CreateAsync(note);
            await repo.SaveAsync();
            await repo.RemoveAsync(note);
            var deleted = await repo.GetAsync("user1", note.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrueIfExists_AndFalseIfNotExistsOrWrongUser()
        {
            var db = GetDbContext(nameof(ExistsAsync_ReturnsTrueIfExists_AndFalseIfNotExistsOrWrongUser));
            var repo = new NoteRepository(db);
            var note = new Note { Name = "Exists", Description = "desc", UserId = "user1" };
            await repo.CreateAsync(note);
            await repo.SaveAsync();
            var exists = await repo.ExistsAsync("user1", note.Id);
            Assert.True(exists);
            var notExists = await repo.ExistsAsync("user2", note.Id);
            Assert.False(notExists);
        }
    }
}
