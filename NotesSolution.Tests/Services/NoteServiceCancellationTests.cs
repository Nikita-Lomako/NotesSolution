using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;
using NotesSolution.Application.Services;
using NotesSolution.Core.Interfaces;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Core.Models;
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
        private readonly Mock<IDistributedCache> _mockCache;
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
            _mockCache = new Mock<IDistributedCache>();

            // Setup cache to return null (cache miss)
            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

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
                _mockCache.Object);
        }

        [Fact]
        public async Task GetAllNotes_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = "test-user";
            var cancellationTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            linkedTokenSource.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cancellationTokenSource);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetAllNotes_WhenTimeoutExpires_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = "test-user";
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            timeoutTokenSource.Cancel();
            linkedTokenSource.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, CancellationToken.None));
        }

        [Fact]
        public async Task CreateNote_WhenCancellationRequestedDuringImageProcessing_ReturnsCancellationError()
        {
            // Arrange
            var userId = "test-user";
            var noteDto = new NoteCreateDto { Name = "Test Note", Description = "Test Description", Tags = new List<string>() };
            var images = new FormFileCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            linkedTokenSource.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cancellationTokenSource);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);

            _createValidatorMock.Setup(v => v.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _tagHelperServiceMock.Setup(s => s.GetOrCreateTagsAsync(noteDto.Tags, userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag>());

            _imageServiceMock.Setup(s => s.SaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var (note, errors) = await _noteService.CreateNote(userId, noteDto, images, cancellationTokenSource.Token);

            // Assert
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
        }

        [Fact]
        public async Task UpdateNote_WhenCancellationRequestedDuringImageProcessing_ReturnsCancellationError()
        {
            // Arrange
            var userId = "test-user";
            var noteId = Guid.NewGuid();
            var noteDto = new NoteUpdateDto { Name = "Updated Note", Description = "Updated Description", Tags = new List<string>() };
            var images = new FormFileCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            linkedTokenSource.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cancellationTokenSource);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);

            _updateValidatorMock.Setup(v => v.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var existingNote = new Note { Id = noteId, UserId = userId, Name = "Original Note", ImageUrls = new List<string>() };
            _noteRepositoryMock.Setup(r => r.GetAsync(userId, noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingNote);

            _tagHelperServiceMock.Setup(s => s.GetOrCreateTagsAsync(noteDto.Tags, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tag>());

            _imageServiceMock.Setup(s => s.SaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
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
