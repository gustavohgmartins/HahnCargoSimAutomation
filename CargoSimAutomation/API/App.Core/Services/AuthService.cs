using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Services;

namespace App.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly ISimulationService simulationService;
        private readonly HahnCargoSimClient _hahnCargoSimClient;
        private readonly Consumer _consumer;

        public AuthService(HahnCargoSimClient hahnCargoSimClient, Consumer consumer, ISimulationService simulationService)
        {
            this.simulationService = simulationService;
            _hahnCargoSimClient = hahnCargoSimClient;
            _consumer = consumer;
        }

        public async Task<AuthDto> Login(AuthDto auth)
        {
            var loginResponse = await _hahnCargoSimClient.Login(auth);

            if (loginResponse != default)
            {
                simulationService.ManageUserAutomation(loginResponse);
            }

            return loginResponse;
        }

        //verifies if the login token is valid
        public async Task<bool> ValidateLogin(string token)
        {
            return await _hahnCargoSimClient.ValidateToken(token);
        }
    }
}
