using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Models;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Services;

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
            // Arrange
            var userId = "user1";
            var notes = new List<Note>
            {
                new Note { Id = Guid.NewGuid(), Name = "Note1", UserId = userId },
                new Note { Id = Guid.NewGuid(), Name = "Note2", UserId = userId },
                new Note { Id = Guid.NewGuid(), Name = "Note3", UserId = "user2" }
            };
            var noteDtos = notes.Where(n => n.UserId == userId).Select(n => new NoteDto { Id = n.Id, Name = n.Name }).ToList();

            _mockNoteRepository.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(notes);
            _mockMapper.Setup(m => m.Map<List<NoteDto>>(It.IsAny<List<Note>>()))
                .Returns(noteDtos);

            // Act
            var result = await _noteService.GetAllNotes(userId, null, null, null, null, 1, 10);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, dto => Assert.Equal(userId, notes.First(n => n.Id == dto.Id).UserId));
        }

        [Fact]
        public async Task GetNoteById_ReturnsNote_WhenNoteExistsAndOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = userId };
            var noteDto = new NoteDto { Id = noteId, Name = "Test Note" };

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync(note);
            _mockMapper.Setup(m => m.Map<NoteDto>(note))
                .Returns(noteDto);

            // Act
            var result = await _noteService.GetNoteById(userId, noteId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(noteId, result.Id);
        }

        [Fact]
        public async Task GetNoteById_ReturnsNull_WhenNoteDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync((Note?)null);

            // Act
            var result = await _noteService.GetNoteById(userId, noteId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteById_ReturnsNull_WhenNoteNotOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = "user2" };

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync(note);

            // Act
            var result = await _noteService.GetNoteById(userId, noteId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateNote_ReturnsValidationErrors_WhenValidationFails()
        {
            // Arrange
            var userId = "user1";
            var noteDto = new NoteCreateDto { Name = "Test", Description = "Test", Tags = new List<string>() };
            var validationErrors = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };

            _mockCreateValidator.Setup(v => v.ValidateAsync(noteDto, default))
                .ReturnsAsync(new ValidationResult(validationErrors));

            // Act
            var result = await _noteService.CreateNote(userId, noteDto, null);
            var (note, errors) = result;

            // Assert
            Assert.Null(note);
            Assert.Single(errors);
            Assert.Contains("Name is required", errors);
        }

        [Fact]
        public async Task CreateNote_CreatesNoteSuccessfully_WhenValidationPasses()
        {
            // Arrange
            var userId = "user1";
            var noteDto = new NoteCreateDto { Name = "Test", Description = "Test", Tags = new List<string> { "tag1" } };
            var note = new Note { Id = Guid.NewGuid(), Name = "Test", UserId = userId };
            var createdNoteDto = new NoteDto { Id = note.Id, Name = "Test" };

            _mockCreateValidator.Setup(v => v.ValidateAsync(noteDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockTagRepository.Setup(r => r.GetByNameAsync("tag1"))
                .ReturnsAsync((Tag?)null);
            _mockMapper.Setup(m => m.Map<Note>(noteDto))
                .Returns(note);
            _mockMapper.Setup(m => m.Map<NoteDto>(note))
                .Returns(createdNoteDto);

            // Act
            var result = await _noteService.CreateNote(userId, noteDto, null);
            var (createdNote, errors) = result;

            // Assert
            Assert.NotNull(createdNote);
            Assert.Empty(errors);
            _mockNoteRepository.Verify(r => r.CreateAsync(It.IsAny<Note>()), Times.Once);
            _mockNoteRepository.Verify(r => r.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var noteDto = new NoteUpdateDto { Name = "Updated", Description = "Updated", Tags = new List<string>() };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(noteDto, default))
                .ReturnsAsync(new ValidationResult());
            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync((Note?)null);

            // Act
            var result = await _noteService.UpdateNote(userId, noteId, noteDto, null);
            var (updatedNote, errors, notFound) = result;

            // Assert
            Assert.Null(updatedNote);
            Assert.True(notFound);
        }

        [Fact]
        public async Task DeleteNote_ReturnsTrue_WhenNoteExistsAndOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = userId };

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync(note);

            // Act
            var result = await _noteService.DeleteNote(userId, noteId);

            // Assert
            Assert.True(result);
            _mockNoteRepository.Verify(r => r.RemoveAsync(note), Times.Once);
        }

        [Fact]
        public async Task DeleteNote_ReturnsFalse_WhenNoteDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync((Note?)null);

            // Act
            var result = await _noteService.DeleteNote(userId, noteId);

            // Assert
            Assert.False(result);
            _mockNoteRepository.Verify(r => r.RemoveAsync(It.IsAny<Note>()), Times.Never);
        }

        [Fact]
        public async Task DeleteNote_ReturnsFalse_WhenNoteNotOwnedByUser()
        {
            // Arrange
            var userId = "user1";
            var noteId = Guid.NewGuid();
            var note = new Note { Id = noteId, Name = "Test Note", UserId = "user2" };

            _mockNoteRepository.Setup(r => r.GetAsync(noteId))
                .ReturnsAsync(note);

            // Act
            var result = await _noteService.DeleteNote(userId, noteId);

            // Assert
            Assert.False(result);
            _mockNoteRepository.Verify(r => r.RemoveAsync(It.IsAny<Note>()), Times.Never);
        }
    }
} 