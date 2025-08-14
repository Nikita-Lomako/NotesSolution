using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Services;
using NotesSolution.Application.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Models;
using AutoMapper;
using FluentValidation;
using System.Threading;
using Xunit;

namespace NotesSolution.Tests.Services
{
    public class NoteServiceCancellationTests
    {
        private readonly Mock<INoteRepository> _noteRepositoryMock;
        private readonly Mock<ITagRepository> _tagRepositoryMock;
        private readonly Mock<IImageService> _imageServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<NoteCreateDto>> _createValidatorMock;
        private readonly Mock<IValidator<NoteUpdateDto>> _updateValidatorMock;
        private readonly Mock<ILogger<NoteService>> _loggerMock;
        private readonly Mock<ITagHelperService> _tagHelperServiceMock;
        private readonly Mock<ICancellationTokenProvider> _cancellationTokenProviderMock;
        private readonly NoteService _noteService;

        public NoteServiceCancellationTests()
        {
            _noteRepositoryMock = new Mock<INoteRepository>();
            _tagRepositoryMock = new Mock<ITagRepository>();
            _imageServiceMock = new Mock<IImageService>();
            _mapperMock = new Mock<IMapper>();
            _createValidatorMock = new Mock<IValidator<NoteCreateDto>>();
            _updateValidatorMock = new Mock<IValidator<NoteUpdateDto>>();
            _loggerMock = new Mock<ILogger<NoteService>>();
            _tagHelperServiceMock = new Mock<ITagHelperService>();
            _cancellationTokenProviderMock = new Mock<ICancellationTokenProvider>();
            var mockCache = new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();

            _noteService = new NoteService(
                _noteRepositoryMock.Object,
                _tagRepositoryMock.Object,
                _imageServiceMock.Object,
                _mapperMock.Object,
                _createValidatorMock.Object,
                _updateValidatorMock.Object,
                _loggerMock.Object,
                _tagHelperServiceMock.Object,
                _cancellationTokenProviderMock.Object,
                mockCache.Object);
        }

        [Fact]
        public async Task GetAllNotes_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = "test-user";
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                cancellationTokenSource.Cancel();
                await _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, cancellationTokenSource.Token);
            });
        }

        [Fact]
        public async Task GetAllNotes_WhenTimeoutExpires_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = "test-user";
            var timeoutTokenSource = new CancellationTokenSource(100); // 100ms timeout
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10);
                await Task.Delay(200); // Wait for timeout
            });
        }

        [Fact]
        public async Task GetAllNotes_WhenOperationCompletesSuccessfully_ReturnsNotes()
        {
            // Arrange
            var userId = "test-user";
            var notes = new List<Note> { new Note { Id = Guid.NewGuid(), Name = "Test Note" } };
            var noteDtos = new List<NoteDto> { new NoteDto { Id = Guid.NewGuid(), Name = "Test Note" } };
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notes);
            _mapperMock.Setup(x => x.Map<List<NoteDto>>(notes))
                .Returns(noteDtos);

            // Act
            var result = await _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(noteDtos[0].Name, result[0].Name);
        }

        [Fact]
        public async Task CreateNote_WhenCancellationRequestedDuringValidation_ReturnsCancellationError()
        {
            // Arrange
            var userId = "test-user";
            var noteDto = new NoteCreateDto { Name = "Test Note", Description = "Test Description" };
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(60000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            _createValidatorMock.Setup(x => x.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var (note, errors) = await _noteService.CreateNote(userId, noteDto, new FormFileCollection(), cancellationTokenSource.Token);

            // Assert
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
        }

        [Fact]
        public async Task CreateNote_WhenCancellationRequestedDuringImageProcessing_ReturnsCancellationError()
        {
            // Arrange
            var userId = "test-user";
            var noteDto = new NoteCreateDto { Name = "Test Note", Description = "Test Description" };
            var images = new FormFileCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(60000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            var validationResult = new FluentValidation.Results.ValidationResult();
            _createValidatorMock.Setup(x => x.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _tagHelperServiceMock.Setup(x => x.GetOrCreateTagsAsync(noteDto.Tags, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var (note, errors) = await _noteService.CreateNote(userId, noteDto, images, cancellationTokenSource.Token);

            // Assert
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
        }

        [Fact]
        public async Task DeleteNote_WhenCancellationRequested_ReturnsFalse()
        {
            // Arrange
            var userId = "test-user";
            var noteId = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            _noteRepositoryMock.Setup(x => x.GetAsync(userId, noteId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _noteService.DeleteNote(userId, noteId, cancellationTokenSource.Token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetNoteById_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = "test-user";
            var noteId = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(15000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            _noteRepositoryMock.Setup(x => x.GetAsync(userId, noteId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                cancellationTokenSource.Cancel();
                await _noteService.GetNoteById(userId, noteId, cancellationTokenSource.Token);
            });
        }

        [Fact]
        public async Task UpdateNote_WhenCancellationRequestedDuringImageProcessing_ReturnsCancellationError()
        {
            // Arrange
            var userId = "test-user";
            var noteId = Guid.NewGuid();
            var noteDto = new NoteUpdateDto { Name = "Updated Note", Description = "Updated Description" };
            var images = new FormFileCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(60000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            var validationResult = new FluentValidation.Results.ValidationResult();
            _updateValidatorMock.Setup(x => x.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var existingNote = new Note { Id = noteId, UserId = userId, Name = "Original Note" };
            _noteRepositoryMock.Setup(x => x.GetAsync(userId, noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingNote);

            _tagHelperServiceMock.Setup(x => x.GetOrCreateTagsAsync(noteDto.Tags, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var (note, errors, notFound) = await _noteService.UpdateNote(userId, noteId, noteDto, images, cancellationTokenSource.Token);

            // Assert
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
            Assert.False(notFound);
        }
    }
} 