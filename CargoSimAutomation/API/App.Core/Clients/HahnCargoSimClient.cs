using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace App.Core.Clients
{
    public class HahnCargoSimClient
    {
        public readonly HttpClient httpClient;
        private readonly string _baseAddress = "https://localhost:7115";
        public HahnCargoSimClient(IHttpClientFactory factory)
        {
            this.httpClient = factory.CreateClient();
        }

        public async Task<AuthDto> Login(AuthDto auth)
        {
            var jsonContent = JsonConvert.SerializeObject(auth);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var responseMsg = await httpClient.PostAsync($"{_baseAddress}/user/login", content);


            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            AuthDto response = JsonConvert.DeserializeObject<AuthDto>(strResponse);

            return response;
        }

        public async Task<bool> ValidateToken(string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/user/CoinAmount");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(requestMessage);

            //var response = await httpClient.GetAsync($"/user/CoinAmount");


            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> StartSimulation(string token)
        {
            //var response = await httpClient.PostAsync($"/sim/start", null);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseAddress}/sim/start");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(requestMessage);


            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> StopSimulation(string token)
        {
            //var response = await httpClient.PostAsync($"/sim/stop", null);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseAddress}/sim/stop");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }
        public async Task<int> GetCoinAmount(string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/user/CoinAmount");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var responseMsg = await httpClient.SendAsync(requestMessage);

            //var responseMsg = await httpClient.GetAsync($"/user/CoinAmount");

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<int>(strResponse);

            return response;
        }

        public async Task<List<Order>> GetAvailableOrders(string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/order/GetAllAvailable");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var responseMsg = await httpClient.SendAsync(requestMessage);

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<List<Order>>(strResponse);

            return response;
        }

        public async Task<List<Order>> GetAcceptedOrders(string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/order/GetAllAccepted");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var responseMsg = await httpClient.SendAsync(requestMessage);

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<List<Order>>(strResponse);

            return response;
        }

        public async Task<bool> AcceptOrder(string token, int orderId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseAddress}/order/Accept");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            requestMessage.Properties.Add("orderId", orderId);
            //requestMessage.Options.Set<int>(new HttpRequestOptionsKey<int>("orderId"), orderId);

            var response = await httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        public async Task<Grid> GetGrid(string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/grid/Get");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            var responseMsg = await httpClient.SendAsync(requestMessage);

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<Grid>(strResponse);

            return response;
        }

        public async Task<int> BuyCargoTransporter(string token, int positionNodeId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseAddress}/CargoTransporter/Buy");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            requestMessage.Properties.Add("positionNodeId", positionNodeId);

            var responseMsg = await httpClient.SendAsync(requestMessage);

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<int>(strResponse);

            return response;
        }

        public async Task<CargoTransporter?> GetCargoTransporter(string token, int transporterId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseAddress}/CargoTransporter/Get");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            requestMessage.Properties.Add("transporterId", transporterId);

            var responseMsg = await httpClient.SendAsync(requestMessage);

            if (!responseMsg.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await responseMsg.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<CargoTransporter?>(strResponse);

            return response;
        }

        public async Task<bool> MoveCargoTransporter(string token, int transporterId, int targetNodeId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseAddress}/CargoTransporter/Move");
            requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            requestMessage.Properties.Add("transporterId", transporterId);
            requestMessage.Properties.Add("targetNodeId", targetNodeId);

            var response = await httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }
    }
}
