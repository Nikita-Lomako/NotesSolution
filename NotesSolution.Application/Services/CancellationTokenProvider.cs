using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NotesSolution.Application.Interfaces;

namespace NotesSolution.Application.Services
{
    /// <summary>
    /// Safe implementation of cancellation token provider with proper resource management
    /// </summary>
    public class CancellationTokenProvider : ICancellationTokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CancellationTokenProvider> _logger;

        public CancellationTokenProvider(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CancellationTokenProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public CancellationToken GetDefaultToken()
        {
            // Get token from HttpContext if available, otherwise use default
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.RequestAborted != null)
            {
                _logger.LogDebug("Using request cancellation token");
                return httpContext.RequestAborted;
            }

            _logger.LogDebug("Using default cancellation token");
            return CancellationToken.None;
        }

        public CancellationTokenSource CreateTimeoutTokenSource(int timeoutMs)
        {
            if (timeoutMs <= 0)
            {
                _logger.LogWarning("Invalid timeout value: {TimeoutMs}, using default timeout of 30 seconds", timeoutMs);
                timeoutMs = 30000; // 30 seconds default
            }

            _logger.LogDebug("Creating timeout token source with {TimeoutMs}ms timeout", timeoutMs);
            return new CancellationTokenSource(timeoutMs);
        }

        public CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                _logger.LogWarning("No tokens provided for linked token source, creating empty source");
                return new CancellationTokenSource();
            }

            _logger.LogDebug("Creating linked token source with {TokenCount} tokens", tokens.Length);
            return CancellationTokenSource.CreateLinkedTokenSource(tokens);
        }
    }
}
