using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Models;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Linq;

namespace NotesSolution.Tests.Services
{
    public class NoteServiceTests
    {
        private readonly Mock<INoteRepository> _mockNoteRepository;
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<IImageService> _mockImageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<NoteCreateDto>> _mockCreateValidator;
        private readonly Mock<IValidator<NoteUpdateDto>> _mockUpdateValidator;
        private readonly Mock<ILogger<NoteService>> _mockLogger;
        private readonly Mock<ITagHelperService> _mockTagHelperService;
        private readonly INoteService _noteService;

        public NoteServiceTests()
        {
            _mockNoteRepository = new Mock<INoteRepository>();
            _mockTagRepository = new Mock<ITagRepository>();
            _mockImageService = new Mock<IImageService>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateValidator = new Mock<IValidator<NoteCreateDto>>();
            _mockUpdateValidator = new Mock<IValidator<NoteUpdateDto>>();
            _mockLogger = new Mock<ILogger<NoteService>>();
            _mockTagHelperService = new Mock<ITagHelperService>();

            _noteService = new NoteService(
                _mockNoteRepository.Object,
                _mockTagRepository.Object,
                _mockImageService.Object,
                _mockMapper.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockLogger.Object,
                _mockTagHelperService.Object
            );
        }

        [Fact]
        public async Task GetAllNotes_ReturnsFilteredNotesForUser()
        {
            var userId = "user1";
            var notes = new List<Note>
            {
                new Note { Id = Guid.NewGuid(), Name = "Note1", UserId = userId },
                new Note { Id = Guid.NewGuid(), Name = "Note2", UserId = userId }
            };
            var noteDtos = notes.Select(n => new NoteDto { Id = n.Id, Name = n.Name }).ToList();
            _mockNoteRepository.Setup(r => r.GetAllAsync(userId, null, null, null, null, 1, 10)).ReturnsAsync(notes);
            _mockMapper.Setup(m => m.Map<List<NoteDto>>(notes)).Returns(noteDtos);
            var result = await _noteService.GetAllNotes(userId, null, null, null, null, 1, 10);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetNoteById_ReturnsNote_WhenNoteExistsAndOwnedByUser()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = userId };
            var noteDto = new NoteDto { Id = noteId, Name = "Test Note" };
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync(note);
            _mockMapper.Setup(m => m.Map<NoteDto>(note)).Returns(noteDto);
            var result = await _noteService.GetNoteById(userId, noteId);
            Assert.NotNull(result);
            Assert.Equal(noteId, result.Id);
        }

        [Fact]
        public async Task GetNoteById_ReturnsNull_WhenNoteDoesNotExist()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync((Note?)null);
            var result = await _noteService.GetNoteById(userId, noteId);
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateNote_ReturnsValidationErrors_WhenValidationFails()
        {
            var userId = "user1";
            var noteDto = new NoteCreateDto { Name = "Test", Description = "Test", Tags = new List<string>() };
            var validationErrors = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };
            _mockCreateValidator.Setup(v => v.ValidateAsync(noteDto, default)).ReturnsAsync(new ValidationResult(validationErrors));
            var result = await _noteService.CreateNote(userId, noteDto, null);
            var (note, errors) = result;
            Assert.Null(note);
            Assert.Single(errors);
            Assert.Contains("Name is required", errors);
        }

        [Fact]
        public async Task CreateNote_CreatesNoteSuccessfully_WhenValidationPasses()
        {
            var userId = "user1";
            var noteDto = new NoteCreateDto { Name = "Test", Description = "Test", Tags = new List<string> { "tag1" } };
            var note = new Note { Id = Guid.NewGuid(), Name = "Test", UserId = userId };
            var createdNoteDto = new NoteDto { Id = note.Id, Name = "Test" };
            _mockCreateValidator.Setup(v => v.ValidateAsync(noteDto, default)).ReturnsAsync(new ValidationResult());
            _mockTagHelperService.Setup(s => s.GetOrCreateTagsAsync(noteDto.Tags, userId)).ReturnsAsync(new List<Tag>());
            _mockMapper.Setup(m => m.Map<Note>(noteDto)).Returns(note);
            _mockMapper.Setup(m => m.Map<NoteDto>(note)).Returns(createdNoteDto);
            _mockNoteRepository.Setup(r => r.CreateAsync(note)).Returns(Task.CompletedTask);
            var result = await _noteService.CreateNote(userId, noteDto, null);
            var (createdNote, errors) = result;
            Assert.NotNull(createdNote);
            Assert.Empty(errors);
            _mockNoteRepository.Verify(r => r.CreateAsync(It.IsAny<Note>()), Times.Once);
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var noteDto = new NoteUpdateDto { Name = "Updated", Description = "Updated", Tags = new List<string>() };
            _mockUpdateValidator.Setup(v => v.ValidateAsync(noteDto, default)).ReturnsAsync(new ValidationResult());
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync((Note?)null);
            var result = await _noteService.UpdateNote(userId, noteId, noteDto, null);
            var (updatedNote, errors, notFound) = result;
            Assert.Null(updatedNote);
            Assert.True(notFound);
        }

        [Fact]
        public async Task UpdateNote_UpdatesNoteSuccessfully_WhenValidationPassesAndNoteExists()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var noteDto = new NoteUpdateDto { Name = "Updated", Description = "Updated", Tags = new List<string> { "tag1" } };
            var note = new Note { Id = noteId, Name = "Old", UserId = userId, Tags = new List<Tag>() };
            var updatedNoteDto = new NoteDto { Id = noteId, Name = "Updated" };
            _mockUpdateValidator.Setup(v => v.ValidateAsync(noteDto, default)).ReturnsAsync(new ValidationResult());
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync(note);
            _mockTagHelperService.Setup(s => s.GetOrCreateTagsAsync(noteDto.Tags, userId)).ReturnsAsync(new List<Tag>());
            _mockMapper.Setup(m => m.Map(noteDto, note)).Verifiable();
            _mockNoteRepository.Setup(r => r.UpdateAsync(note)).Returns(Task.CompletedTask);
            _mockNoteRepository.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<NoteDto>(note)).Returns(updatedNoteDto);
            var result = await _noteService.UpdateNote(userId, noteId, noteDto, null);
            var (updatedNote, errors, notFound) = result;
            Assert.NotNull(updatedNote);
            Assert.Empty(errors);
            Assert.False(notFound);
            _mockNoteRepository.Verify(r => r.UpdateAsync(note), Times.Once);
            _mockNoteRepository.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteNote_ReturnsTrue_WhenNoteExistsAndOwnedByUser()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = userId, ImageUrls = new List<string> { "/images/img1.png" } };
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync(note);
            _mockNoteRepository.Setup(r => r.RemoveAsync(note)).Returns(Task.CompletedTask);
            _mockImageService.Setup(s => s.DeleteImage(It.IsAny<string>())).Returns(true);
            var result = await _noteService.DeleteNote(userId, noteId);
            Assert.True(result);
            _mockNoteRepository.Verify(r => r.RemoveAsync(note), Times.Once);
            _mockImageService.Verify(s => s.DeleteImage(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteNote_ReturnsFalse_WhenNoteDoesNotExist()
        {
            var userId = "user1";
            var noteId = Guid.NewGuid();
            _mockNoteRepository.Setup(r => r.GetAsync(userId, noteId)).ReturnsAsync((Note?)null);
            var result = await _noteService.DeleteNote(userId, noteId);
            Assert.False(result);
            _mockNoteRepository.Verify(r => r.RemoveAsync(It.IsAny<Note>()), Times.Never);
        }
    }
} 