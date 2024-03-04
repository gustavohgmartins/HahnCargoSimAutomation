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
            this.Authinfo = await hahnCargoSimClient.Login(auth);

            return this.Authinfo;
        }

        public async Task<int> VerifyLogin()
        {
            return await hahnCargoSimClient.CoinAmount();
        }
    }
}
