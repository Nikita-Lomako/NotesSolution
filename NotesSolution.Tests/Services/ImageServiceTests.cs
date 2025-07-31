using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NotesSolution.Infrastructure.Services;
using Xunit;

namespace NotesSolution.Tests.Services
{
    public class ImageServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly LocalImageService _service;
        private readonly string _baseUrl = "http://localhost";

        public ImageServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<LocalImageService>>();
            _service = new LocalImageService(_tempDir, _baseUrl, logger);
        }

        [Fact]
        public async Task SaveImageAsync_SavesFileAndReturnsUrl()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.png";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns((Stream s, System.Threading.CancellationToken t) => ms.CopyToAsync(s));

            var url = await _service.SaveImageAsync(fileMock.Object, CancellationToken.None);
            Assert.StartsWith("/images/", url);
            var filePath = Path.Combine(_tempDir, url.Replace("/images/", ""));
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void DeleteImage_DeletesFile()
        {
            var fileName = "todelete.png";
            var filePath = Path.Combine(_tempDir, fileName);
            File.WriteAllText(filePath, "test");
            var url = $"/images/{fileName}";
            var result = _service.DeleteImage(url);
            Assert.True(result);
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task ComputeImageHashAsync_ReturnsHash()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "hash me";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            var hash = await _service.ComputeImageHashAsync(fileMock.Object, CancellationToken.None);
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Equal(64, hash.Length); // SHA256 hash length in hex
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
    }
}
