using App.Core.Clients;
using App.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core.Services
{
    public class AuthService
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        public AuthDto Authinfo { get; private set; }

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
