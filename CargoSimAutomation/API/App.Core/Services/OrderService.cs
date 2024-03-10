using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Services;

namespace App.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly ISimulationService simulationService;
        private readonly HahnCargoSimClient _hahnCargoSimClient;
        private readonly Consumer _consumer;

        public OrderService(HahnCargoSimClient hahnCargoSimClient, Consumer consumer, ISimulationService simulationService)
        {
            this.simulationService = simulationService;
            _hahnCargoSimClient = hahnCargoSimClient;
            _consumer = consumer;
        }

        public async Task<bool> Generate(string token)
        {
            var loginResponse = await _hahnCargoSimClient.CreateOrder(token);

            return loginResponse;
        }
    }
}
