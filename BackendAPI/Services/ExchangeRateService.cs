using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackendAPI.Services
{
    public interface IExchangeRateService
    {
        Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency, string symbols);
        string GetStrongestCurrency(Dictionary<string, decimal> rates);
        string GetWeakestCurrency(Dictionary<string, decimal> rates);
        decimal GetAverageRate(Dictionary<string, decimal> rates);
    }

    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;

        public ExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency, string symbols)
        {
            // Volání externího API podle dokumentu DSP
            var url = $"https://api.exchangerate.host/latest?base={baseCurrency}&symbols={symbols}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);
            
            var success = document.RootElement.GetProperty("success").GetBoolean();
            if (!success)
            {
                throw new Exception("API vrátil neúspěšný status.");
            }

            var ratesElement = document.RootElement.GetProperty("rates");
            var rates = new Dictionary<string, decimal>();

            // Rozparsování JSON odpovědi
            foreach (var property in ratesElement.EnumerateObject())
            {
                rates.Add(property.Name, property.Value.GetDecimal());
            }

            return rates;
        }

        public string GetStrongestCurrency(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return string.Empty;
            // Nejsilnější měna má nejvyšší nominální hodnotu
            return rates.OrderByDescending(r => r.Value).First().Key;
        }

        public string GetWeakestCurrency(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return string.Empty;
            // Nejslabší měna má nejnižší nominální hodnotu
            return rates.OrderBy(r => r.Value).First().Key;
        }

        public decimal GetAverageRate(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return 0;
            // Aritmetický průměr, chybějící data se ignorují (pokud by chyběla, nenačtou se do Dictionary)
            return rates.Values.Average();
        }
    }
}