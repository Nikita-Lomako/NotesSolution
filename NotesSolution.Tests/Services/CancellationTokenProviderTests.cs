using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Application.Services;
using NotesSolution.Application.Interfaces;
using System.Threading;
using Xunit;

namespace NotesSolution.Tests.Services
{
    public class CancellationTokenProviderTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<CancellationTokenProvider>> _loggerMock;
        private readonly CancellationTokenProvider _provider;

        public CancellationTokenProviderTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<CancellationTokenProvider>>();
            _provider = new CancellationTokenProvider(_httpContextAccessorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void GetDefaultToken_WhenHttpContextExists_ReturnsRequestAbortedToken()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var cancellationTokenSource = new CancellationTokenSource();
            httpContext.RequestAborted = cancellationTokenSource.Token;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _provider.GetDefaultToken();

            // Assert
            Assert.Equal(cancellationTokenSource.Token, result);
        }

        [Fact]
        public void GetDefaultToken_WhenHttpContextIsNull_ReturnsNoneToken()
        {
            // Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

            // Act
            var result = _provider.GetDefaultToken();

            // Assert
            Assert.Equal(CancellationToken.None, result);
        }

        [Fact]
        public void CreateTimeoutTokenSource_WithValidTimeout_CreatesTokenSource()
        {
            // Arrange
            var timeoutMs = 5000;

            // Act
            using var result = _provider.CreateTimeoutTokenSource(timeoutMs);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateTimeoutTokenSource_WithInvalidTimeout_UsesDefaultTimeout()
        {
            // Arrange
            var invalidTimeoutMs = -1000;

            // Act
            using var result = _provider.CreateTimeoutTokenSource(invalidTimeoutMs);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateLinkedTokenSource_WithMultipleTokens_CreatesLinkedTokenSource()
        {
            // Arrange
            var token1 = new CancellationTokenSource().Token;
            var token2 = new CancellationTokenSource().Token;

            // Act
            using var result = _provider.CreateLinkedTokenSource(token1, token2);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateLinkedTokenSource_WithNoTokens_CreatesEmptyTokenSource()
        {
            // Act
            using var result = _provider.CreateLinkedTokenSource();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateTimeoutTokenSource_TokenExpiresAfterTimeout()
        {
            // Arrange
            var timeoutMs = 100; // 100ms timeout

            // Act
            using var tokenSource = _provider.CreateTimeoutTokenSource(timeoutMs);
            
            // Wait for timeout
            Thread.Sleep(200);

            // Assert
            Assert.True(tokenSource.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateLinkedTokenSource_WhenOneTokenCancelled_LinkedTokenCancelled()
        {
            // Arrange
            var tokenSource1 = new CancellationTokenSource();
            var tokenSource2 = new CancellationTokenSource();

            // Act
            using var linkedTokenSource = _provider.CreateLinkedTokenSource(tokenSource1.Token, tokenSource2.Token);
            
            // Cancel one of the tokens
            tokenSource1.Cancel();

            // Assert
            Assert.True(linkedTokenSource.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateLinkedTokenSource_WithCancellationTokenNone_WorksCorrectly()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();

            // Act
            using var linkedTokenSource = _provider.CreateLinkedTokenSource(CancellationToken.None, tokenSource.Token);

            // Assert
            Assert.NotNull(linkedTokenSource);
            Assert.False(linkedTokenSource.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateTimeoutTokenSource_WithZeroTimeout_UsesDefaultTimeout()
        {
            // Arrange
            var zeroTimeoutMs = 0;

            // Act
            using var result = _provider.CreateTimeoutTokenSource(zeroTimeoutMs);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }

        [Fact]
        public void CreateLinkedTokenSource_WithNullTokens_CreatesEmptyTokenSource()
        {
            // Act
            using var result = _provider.CreateLinkedTokenSource(Array.Empty<CancellationToken>());

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Token.IsCancellationRequested);
        }
    }
} 