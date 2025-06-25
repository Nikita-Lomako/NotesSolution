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
            var fetched = await repo.GetAsync(note.Id);
            Assert.NotNull(fetched);
            Assert.Equal("TestNote", fetched.Name);
        }

        [Fact]
        public async Task GetAllAsync_FiltersAndPaginates()
        {
            var db = GetDbContext(nameof(GetAllAsync_FiltersAndPaginates));
            var repo = new NoteRepository(db);
            for (int i = 1; i <= 15; i++)
            {
                await repo.CreateAsync(new Note { Name = $"Note{i}", Description = "desc", UserId = "user1" });
            }
            await repo.SaveAsync();
            var notes = await repo.GetAllAsync(null, null, null, null, 2, 5); // page 2, pageSize 5
            Assert.Equal(5, notes.Count);
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
            var updated = await repo.GetAsync(note.Id);
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
            var deleted = await repo.GetAsync(note.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrueIfExists()
        {
            var db = GetDbContext(nameof(ExistsAsync_ReturnsTrueIfExists));
            var repo = new NoteRepository(db);
            var note = new Note { Name = "Exists", Description = "desc", UserId = "user1" };
            await repo.CreateAsync(note);
            await repo.SaveAsync();
            var exists = await repo.ExistsAsync(note.Id);
            Assert.True(exists);
        }
    }
}
