using Microsoft.AspNetCore.Http;
using NotesSolution.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesSolution.Infrastructure.Services
{
    public class LocalImageService : IImageService
    {
        private readonly string _imagePath;
        private readonly string _baseUrl;

        public LocalImageService(string imagePath, string baseUrl)
        {
            _imagePath = imagePath;
            _baseUrl = baseUrl;

            if (!Directory.Exists(_imagePath))
                Directory.CreateDirectory(_imagePath);
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_imagePath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{_baseUrl}/{fileName}";
        }

        public bool DeleteImage(string imageUrl)
        {
            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(_imagePath, fileName);

            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
    }
}
