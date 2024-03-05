using App.Domain.DTO;

namespace App.Domain.Services
{
    public interface IAuthService
    {
        Task<AuthDto> Login(AuthDto auth);
        Task<bool> ValidateLogin(string token);
    }
}
