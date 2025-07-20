using NotesSolution.Application.Dtos;
using System.Threading.Tasks;

namespace NotesSolution.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
        Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto);
    }
} 