using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Services;
using Newtonsoft.Json.Linq;


namespace App.Core.Services
{
    public class Automation: IAutomation
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly AuthDto authUser;
        private string _token;

        private bool _isRunning;

        public Automation(HahnCargoSimClient hahnCargoSimClient, AuthDto authUser)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.authUser = authUser;
        }

        public async Task Start(string token)
        {
            _token = token;

            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            Task.Run(ExecuteAsync);
        }

        public async Task Stop()
        {
            _isRunning = false;
        }

        private async Task ExecuteAsync()
        {
            while (_isRunning)
            {
                var coins = await hahnCargoSimClient.GetCoinAmount(_token);
                Console.WriteLine($"Hello, {authUser.Username}! You have {coins} coins!");

                await Task.Delay(3000);
            }
        }
    }
}
