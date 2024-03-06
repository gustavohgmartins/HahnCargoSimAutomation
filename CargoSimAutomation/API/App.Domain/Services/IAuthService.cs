using App.Domain.DTOs;

namespace App.Domain.Services
{
    public interface IAuthService
    {
        Task<AuthDto> Login(AuthDto auth);
        Task<bool> ValidateLogin(string token);
    }
}
