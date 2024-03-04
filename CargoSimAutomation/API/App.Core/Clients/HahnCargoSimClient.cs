using App.Core.Services;
using App.Domain.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", responseDto.Token);

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


            return JsonConvert.DeserializeObject<int>(strResponse); ;
        }
    }
}
