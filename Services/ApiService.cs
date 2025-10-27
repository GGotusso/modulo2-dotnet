using System.Net.Http;
using Modulo2Web.Config;
using Modulo2Web.Models;
using Newtonsoft.Json;

namespace Modulo2Web.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, AppConfig config)
        {
            _httpClient = httpClient;
            _baseUrl = config.Api.Url;
        }

        public async Task<Vehicle?> BuscarVehiculoPorPatenteAsync(string patente)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/vehicles?plate={patente}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var vehicleResponse = JsonConvert.DeserializeObject<VehicleResponse>(content);

                if (vehicleResponse?.data != null && vehicleResponse.data.Count > 0)
                {
                    return vehicleResponse.data[0];
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al buscar vehículo: {ex.Message}");
                throw;
            }
        }

        public async Task<Customer?> BuscarClientePorIdAsync(string customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/customers?customer_id={customerId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<Customer>(content);

                return customer;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al buscar cliente: {ex.Message}");
                throw;
            }
        }
    }
}
