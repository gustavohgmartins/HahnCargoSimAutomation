using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Services;
using Newtonsoft.Json.Linq;


namespace App.Core.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly Consumer consumer;

        private bool _isRunning;

        public SimulationService(HahnCargoSimClient hahnCargoSimClient, Consumer consumer)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.consumer = consumer;
        }

        public async Task<bool> Start(string token, string username)
        {
            var response = await hahnCargoSimClient.StartSimulation(token);

            if (response)
            {
                var automation = ManageUserAutomation(new AuthDto() { Username = username });

                await automation.Start(token);
            }
            
            return response;
        }

        public async Task<bool> Stop(string token)
        {
            var response = await hahnCargoSimClient.StopSimulation(token);

            if (response)
            {
                _ = AutomationDictionary.UserAutomation.Values.Select(x => x.Stop()).ToList();
            }

            return response;
        }

        //checks if the user already has an Automation; if not, creates one.
        public IAutomation ManageUserAutomation(AuthDto authUser)
        {
            var userAutomation = AutomationDictionary.GetUserAutomation(authUser.Username);

            if (userAutomation == default)
            {
                userAutomation = new Automation(hahnCargoSimClient, authUser);

                AutomationDictionary.AddUserAutomation(authUser.Username, userAutomation);
            }
            var teste = AutomationDictionary.UserAutomation.Values;

            return userAutomation;

        }
    }
}
