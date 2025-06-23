using NotesSolution.Core.Dtos;

namespace NotesSolution.Core.Interfaces.IRepositories
{
    public interface IAuthRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDto?> Login(LoginRequestDto loginRequestDto);
        Task<UserDto?> Register(RegistrationRequestDto requestDto);
    }
} 