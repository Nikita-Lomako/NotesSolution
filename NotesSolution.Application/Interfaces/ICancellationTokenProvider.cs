using System.Threading;

namespace NotesSolution.Application.Interfaces
{
    /// <summary>
    /// Provides centralized management of cancellation tokens
    /// </summary>
    public interface ICancellationTokenProvider
    {
        /// <summary>
        /// Gets the default cancellation token for the current request
        /// </summary>
        CancellationToken GetDefaultToken();
        
        /// <summary>
        /// Creates a new cancellation token source with timeout
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>Cancellation token source</returns>
        CancellationTokenSource CreateTimeoutTokenSource(int timeoutMs);
        
        /// <summary>
        /// Creates a linked cancellation token source
        /// </summary>
        /// <param name="tokens">Tokens to link</param>
        /// <returns>Linked cancellation token source</returns>
        CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens);
    }
} 