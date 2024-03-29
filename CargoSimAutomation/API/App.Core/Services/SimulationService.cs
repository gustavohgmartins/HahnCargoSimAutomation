﻿using App.Core.Clients;
using App.Core.Hubs;
using App.Domain.DTOs;
using App.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;


namespace App.Core.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly Consumer consumer;
        private readonly AutomationHub hub;
        private readonly IConfiguration configuration;
        private bool _isRunning;

        public SimulationService(HahnCargoSimClient hahnCargoSimClient, Consumer consumer, IConfiguration configuration, AutomationHub hub)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.consumer = consumer;
            this.hub = hub;
            this.configuration = configuration.GetSection("SimulationConfig");
        }

        public async Task<bool> Start(string token, string username)
        {
            var response = await hahnCargoSimClient.StartSimulation(token);

            if (response)
            {
                Task.Run(async () => await consumer.StartConsuming());

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
                consumer.StopConsuming();

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
                userAutomation = new Automation(hahnCargoSimClient, authUser, configuration, hub, consumer);

                AutomationDictionary.AddUserAutomation(authUser.Username, userAutomation);
            }
            var teste = AutomationDictionary.UserAutomation.Values;

            return userAutomation;
        }
    }
}
