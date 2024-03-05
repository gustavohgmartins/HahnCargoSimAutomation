using App.Core.Clients;
using App.Domain.DTO;
using App.Domain.Services;

namespace App.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;

        private AuthDto authDto;

        public AuthService(HahnCargoSimClient hahnCargoSimClient)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
        }

        public async Task<AuthDto> Login(AuthDto auth)
        {
            return await hahnCargoSimClient.Login(auth);
        }

        public async Task<int> VerifyLogin(string token)
        {
            hahnCargoSimClient.SetToken(token);

            return await hahnCargoSimClient.CoinAmount();
        }
    }
}
