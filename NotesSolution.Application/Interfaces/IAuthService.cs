using System.Threading;
using System.Threading.Tasks;
using NotesSolution.Application.Dtos;

namespace NotesSolution.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default);
        Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
