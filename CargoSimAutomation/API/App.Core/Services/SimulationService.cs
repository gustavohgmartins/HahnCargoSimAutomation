using App.Core.Clients;
using App.Domain.DTO;
using App.Domain.Services;


namespace App.Core.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly Consumer consumer;

        public AuthDto Authinfo { get; private set; }

        public SimulationService(HahnCargoSimClient hahnCargoSimClient, Consumer consumer)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.consumer = consumer;
        }

        public async Task<bool> Start(string token)
        {
            hahnCargoSimClient.SetToken(token);

            var response = await hahnCargoSimClient.StartSimulation();

            if (response)
            {
                //keeps trying to start consuming in the background
                Task.Run(() => consumer.StartConsuming());
            }

            return response;
        }

        public async Task<bool> Stop(string token)
        {
            hahnCargoSimClient.SetToken(token);

            var response = await hahnCargoSimClient.StopSimulation();


            if (response)
            {
                consumer.StopConsuming();
            }

            return response;
        }
    }
}
