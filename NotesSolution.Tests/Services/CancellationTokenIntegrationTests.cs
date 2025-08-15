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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;

namespace NotesSolution.Tests.Services
{
    public class CancellationTokenIntegrationTests
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

        public CancellationTokenIntegrationTests()
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
        public async Task ResourceCleanup_WhenCancellationRequested_ResourcesAreReleased()
        {
            // Arrange
            var userId = "test-user";
            var cts = new CancellationTokenSource();
            var linkedCts = new CancellationTokenSource();
            linkedCts.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cts);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedCts);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, cts.Token));
        }

        [Fact]
        public async Task ImageProcessing_WhenCancellationRequested_ImagesAreNotSaved()
        {
            // Arrange
            var userId = "test-user";
            var noteDto = new NoteCreateDto { Name = "Test Note", Description = "Test Description", Tags = new List<string>() };
            var images = new FormFileCollection();
            var cts = new CancellationTokenSource();
            var linkedCts = new CancellationTokenSource();
            linkedCts.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cts);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedCts);

            _createValidatorMock.Setup(v => v.ValidateAsync(noteDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _tagHelperServiceMock.Setup(s => s.GetOrCreateTagsAsync(noteDto.Tags, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tag>());
            _imageServiceMock.Setup(s => s.SaveImageAsync(It.IsAny<IFormFile>(), linkedCts.Token))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var (note, errors) = await _noteService.CreateNote(userId, noteDto, images, cts.Token);

            // Assert
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
        }

        [Fact]
        public async Task DatabaseConnection_WhenCancellationRequested_ConnectionIsReleased()
        {
            // Arrange
            var userId = "test-user";
            var cts = new CancellationTokenSource();
            var linkedCts = new CancellationTokenSource();
            linkedCts.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cts);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedCts);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, cts.Token));
        }

        [Fact]
        public void HttpContextIsolation_EachRequestHasIndependentToken()
        {
            // Arrange
            var httpContextAccessor1 = new Mock<IHttpContextAccessor>();
            var httpContextAccessor2 = new Mock<IHttpContextAccessor>();
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            httpContextAccessor1.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { RequestAborted = cts1.Token });
            httpContextAccessor2.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { RequestAborted = cts2.Token });

            var provider1 = new CancellationTokenProvider(httpContextAccessor1.Object, Mock.Of<ILogger<CancellationTokenProvider>>());
            var provider2 = new CancellationTokenProvider(httpContextAccessor2.Object, Mock.Of<ILogger<CancellationTokenProvider>>());

            // Act
            var token1 = provider1.GetDefaultToken();
            var token2 = provider2.GetDefaultToken();

            cts1.Cancel();

            // Assert
            Assert.True(token1.IsCancellationRequested);
            Assert.False(token2.IsCancellationRequested);
        }

        [Fact]
        public async Task TimeoutExpiration_WhenTimeoutExpires_OperationIsCancelled()
        {
            // Arrange
            var userId = "test-user";
            var timeoutCts = new CancellationTokenSource();
            var linkedCts = new CancellationTokenSource();
            timeoutCts.Cancel();
            linkedCts.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(timeoutCts);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedCts);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, CancellationToken.None));
        }

        [Fact]
        public async Task LinkedTokens_WhenAnyTokenCancelled_OperationIsCancelled()
        {
            // Arrange
            var userId = "test-user";
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);
            linkedCts.Cancel();

            _cancellationTokenProviderMock.Setup(p => p.GetDefaultToken())
                .Returns(CancellationToken.None);
            _cancellationTokenProviderMock.Setup(p => p.CreateTimeoutTokenSource(It.IsAny<int>()))
                .Returns(cts2);
            _cancellationTokenProviderMock.Setup(p => p.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedCts);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _noteService.GetAllNotes(userId, string.Empty, string.Empty, string.Empty, string.Empty, 1, 10, CancellationToken.None));
        }
    }
}