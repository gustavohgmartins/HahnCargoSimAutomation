using App.Domain.DTO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace App.Core.Clients
{
    public class HahnCargoSimClient
    {
        public const string Name = "HahnCargoSimClient";

        private readonly HttpClient httpClient;

        public HahnCargoSimClient(IHttpClientFactory factory)
        {
            this.httpClient = factory.CreateClient(Name);
        }

        public void SetToken(string token)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AuthDto> Login(AuthDto auth)
        {
            var jsonContent = JsonConvert.SerializeObject(auth);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"/user/login", content);


            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await response.Content.ReadAsStringAsync();

            AuthDto responseDto = JsonConvert.DeserializeObject<AuthDto>(strResponse);

            return responseDto;
        }

        public async Task<int> CoinAmount()
        {
            var response = await httpClient.GetAsync($"/user/CoinAmount");


            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var strResponse = await response.Content.ReadAsStringAsync();


            return JsonConvert.DeserializeObject<int>(strResponse);
        }

        public async Task<bool> StartSimulation()
        {
            var response = await httpClient.PostAsync($"/sim/start", null);


            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> StopSimulation()
        {
            var response = await httpClient.PostAsync($"/sim/stop", null);


            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }
    }
}
