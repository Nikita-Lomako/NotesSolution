using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotesSolution.Core.Interfaces
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);
        bool DeleteImage(string imageUrl);
        Task<string> ComputeImageHashAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
