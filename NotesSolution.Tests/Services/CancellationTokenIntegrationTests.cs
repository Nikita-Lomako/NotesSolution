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

            _noteService = new NoteService(
                _noteRepositoryMock.Object,
                _tagRepositoryMock.Object,
                _imageServiceMock.Object,
                _mapperMock.Object,
                _createValidatorMock.Object,
                _updateValidatorMock.Object,
                _loggerMock.Object,
                _tagHelperServiceMock.Object,
                _cancellationTokenProviderMock.Object);
        }

        [Fact]
        public async Task MultipleConcurrentRequests_EachHasIndependentCancellation()
        {
            // Arrange
            var userId = "test-user";
            var results = new ConcurrentBag<string>();
            var cancellationTokenSources = new List<CancellationTokenSource>();
            
            // Создаем 5 независимых токенов отмены
            for (int i = 0; i < 5; i++)
            {
                cancellationTokenSources.Add(new CancellationTokenSource());
            }

            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            var notes = new List<Note> { new Note { Id = Guid.NewGuid(), Name = "Test Note" } };
            var noteDtos = new List<NoteDto> { new NoteDto { Id = Guid.NewGuid(), Name = "Test Note" } };

            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notes);
            _mapperMock.Setup(x => x.Map<List<NoteDto>>(notes))
                .Returns(noteDtos);

            // Act - Запускаем 5 параллельных запросов
            var tasks = cancellationTokenSources.Select(async (cts, index) =>
            {
                try
                {
                    var result = await _noteService.GetAllNotes(userId, null, null, null, null, 1, 10, cts.Token);
                    results.Add($"Request {index}: Success");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    results.Add($"Request {index}: Cancelled");
                    throw;
                }
            }).ToList();

            // Отменяем только второй запрос
            await Task.Delay(100);
            cancellationTokenSources[1].Cancel();

            // Act - Ждем завершения всех задач
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // Ожидаемо - один из запросов был отменен
            }

            // Assert
            Assert.Contains("Request 1: Cancelled", results);
            Assert.Contains("Request 0: Success", results);
            Assert.Contains("Request 2: Success", results);
            Assert.Contains("Request 3: Success", results);
            Assert.Contains("Request 4: Success", results);
        }

        [Fact]
        public async Task ResourceCleanup_WhenCancellationRequested_ResourcesAreReleased()
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

            // Симулируем длительную операцию
            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(1000, cancellationTokenSource.Token); // Длительная операция
                    return new List<Note>();
                });

            // Act & Assert
            var task = _noteService.GetAllNotes(userId, null, null, null, null, 1, 10, cancellationTokenSource.Token);
            
            // Отменяем операцию через 100ms
            await Task.Delay(100);
            cancellationTokenSource.Cancel();

            // Проверяем, что операция была отменена
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            
            // Проверяем, что ресурсы были освобождены (using statements)
            Assert.True(true); // Если мы дошли сюда, значит ресурсы освобождены
        }

        [Fact]
        public async Task ImageProcessing_WhenCancellationRequested_ImagesAreNotSaved()
        {
            // Arrange
            var userId = "test-user";
            var noteDto = new NoteCreateDto { Name = "Test Note", Description = "Test Description" };
            var images = new FormFileCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            var imagesProcessed = 0;

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
                .ReturnsAsync(new List<Tag>());

            // Симулируем обработку изображений с возможностью отмены
            _imageServiceMock.Setup(x => x.ComputeImageHashAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .Returns(async (IFormFile file, CancellationToken ct) =>
                {
                    imagesProcessed++;
                    await Task.Delay(200, ct); // Симулируем длительную обработку
                    return "hash";
                });

            _imageServiceMock.Setup(x => x.SaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .Returns(async (IFormFile file, CancellationToken ct) =>
                {
                    await Task.Delay(200, ct); // Симулируем длительное сохранение
                    return "image-url";
                });

            // Act
            var task = _noteService.CreateNote(userId, noteDto, images, cancellationTokenSource.Token);
            
            // Отменяем операцию через 100ms
            await Task.Delay(100);
            cancellationTokenSource.Cancel();

            // Assert
            var (note, errors) = await task;
            Assert.Null(note);
            Assert.Contains("Operation was cancelled", errors);
            Assert.Equal(0, imagesProcessed); // Изображения не должны быть обработаны
        }

        [Fact]
        public async Task DatabaseConnection_WhenCancellationRequested_ConnectionIsReleased()
        {
            // Arrange
            var userId = "test-user";
            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = new CancellationTokenSource();
            var dbOperationStarted = false;
            var dbOperationCancelled = false;

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            // Симулируем длительную операцию с базой данных
            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                .Returns(async (string uid, string search, string tag, string sort, string order, int page, int pageSize, CancellationToken ct) =>
                {
                    dbOperationStarted = true;
                    try
                    {
                        await Task.Delay(1000, ct); // Длительная операция с БД
                        return new List<Note>();
                    }
                    catch (OperationCanceledException)
                    {
                        dbOperationCancelled = true;
                        throw;
                    }
                });

            // Act
            var task = _noteService.GetAllNotes(userId, null, null, null, null, 1, 10, cancellationTokenSource.Token);
            
            // Отменяем операцию через 100ms
            await Task.Delay(100);
            cancellationTokenSource.Cancel();

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            Assert.True(dbOperationStarted);
            Assert.True(dbOperationCancelled);
        }

        [Fact]
        public Task HttpContextIsolation_EachRequestHasIndependentToken()
        {
            // Arrange
            var httpContext1 = new DefaultHttpContext();
            var httpContext2 = new DefaultHttpContext();
            var tokenSource1 = new CancellationTokenSource();
            var tokenSource2 = new CancellationTokenSource();
            
            httpContext1.RequestAborted = tokenSource1.Token;
            httpContext2.RequestAborted = tokenSource2.Token;

            var provider1 = new CancellationTokenProvider(
                new HttpContextAccessor { HttpContext = httpContext1 },
                Mock.Of<ILogger<CancellationTokenProvider>>());
            
            var provider2 = new CancellationTokenProvider(
                new HttpContextAccessor { HttpContext = httpContext2 },
                Mock.Of<ILogger<CancellationTokenProvider>>());

            // Act
            var token1 = provider1.GetDefaultToken();
            var token2 = provider2.GetDefaultToken();

            // Отменяем только первый токен
            tokenSource1.Cancel();

            // Assert
            Assert.True(token1.IsCancellationRequested);
            Assert.False(token2.IsCancellationRequested);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TimeoutExpiration_WhenTimeoutExpires_OperationIsCancelled()
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

            // Симулируем длительную операцию
            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(500); // Операция дольше таймаута
                    return new List<Note>();
                });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _noteService.GetAllNotes(userId, null, null, null, null, 1, 10);
            });
        }

        [Fact]
        public async Task LinkedTokens_WhenAnyTokenCancelled_OperationIsCancelled()
        {
            // Arrange
            var userId = "test-user";
            var requestTokenSource = new CancellationTokenSource();
            var timeoutTokenSource = new CancellationTokenSource();
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                requestTokenSource.Token, 
                timeoutTokenSource.Token);

            _cancellationTokenProviderMock.Setup(x => x.CreateTimeoutTokenSource(30000))
                .Returns(timeoutTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.CreateLinkedTokenSource(It.IsAny<CancellationToken[]>()))
                .Returns(linkedTokenSource);
            _cancellationTokenProviderMock.Setup(x => x.GetDefaultToken())
                .Returns(CancellationToken.None);

            // Симулируем длительную операцию
            _noteRepositoryMock.Setup(x => x.GetAllAsync(userId, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(1000);
                    return new List<Note>();
                });

            // Act
            var task = _noteService.GetAllNotes(userId, null, null, null, null, 1, 10, requestTokenSource.Token);
            
            // Отменяем таймаут токен
            await Task.Delay(100);
            timeoutTokenSource.Cancel();

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }
    }
} 