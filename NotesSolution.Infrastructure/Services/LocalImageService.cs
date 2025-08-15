using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NotesSolution.Core.Interfaces;

namespace NotesSolution.Infrastructure.Services
{
    public class LocalImageService : IImageService
    {
        private readonly string _imagePath;
        private readonly string _baseUrl;
        private readonly ILogger<LocalImageService> _logger;

        public LocalImageService(string imagePath, string baseUrl, ILogger<LocalImageService> logger)
        {
            _imagePath = imagePath;
            _baseUrl = baseUrl;
            _logger = logger;

            if (!Directory.Exists(_imagePath))
            {
                Directory.CreateDirectory(_imagePath);
                _logger.LogInformation("Created image directory at {ImagePath}", _imagePath);
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cancellation before starting file operation
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_imagePath, fileName);

                _logger.LogDebug("Saving image {FileName} to {FilePath}", fileName, filePath);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream, cancellationToken);

                var imageUrl = $"/images/{fileName}";
                _logger.LogDebug("Successfully saved image {FileName} as {ImageUrl}", fileName, imageUrl);

                return imageUrl;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Image save operation was cancelled for file {FileName}", file.FileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image {FileName}", file.FileName);
                throw;
            }
        }

        public bool DeleteImage(string imageUrl)
        {
            try
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_imagePath, fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Image file not found at {FilePath}", filePath);
                    return false;
                }

                File.Delete(filePath);
                _logger.LogDebug("Successfully deleted image {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageUrl}", imageUrl);
                return false;
            }
        }

        public async Task<string> ComputeImageHashAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cancellation before starting hash computation
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Computing hash for image {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                _logger.LogDebug("Computed hash {Hash} for image {FileName}", hashString, file.FileName);
                return hashString;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Image hash computation was cancelled for file {FileName}", file.FileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing hash for image {FileName}", file.FileName);
                throw;
            }
        }
    }
}
