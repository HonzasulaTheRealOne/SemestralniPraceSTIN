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
            var url = $"https://open.er-api.com/v6/latest/{baseCurrency}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);
            
            var result = document.RootElement.GetProperty("result").GetString();
            if (result != "success")
            {
                throw new Exception("API vrátil neúspěšný status.");
            }

            var allRatesElement = document.RootElement.GetProperty("rates");
            var rates = new Dictionary<string, decimal>();
            
            var requestedSymbols = symbols.Split(',').Select(s => s.Trim().ToUpper()).ToList();

            foreach (var property in allRatesElement.EnumerateObject())
            {
                if (requestedSymbols.Contains(property.Name))
                {
                    rates.Add(property.Name, property.Value.GetDecimal());
                }
            }

            return rates;
        }

        public string GetStrongestCurrency(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return string.Empty;
            return rates.OrderByDescending(r => r.Value).First().Key;
        }

        public string GetWeakestCurrency(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return string.Empty;
            return rates.OrderBy(r => r.Value).First().Key;
        }

        public decimal GetAverageRate(Dictionary<string, decimal> rates)
        {
            if (rates == null || rates.Count == 0) return 0;
            return rates.Values.Average();
        }
    }
}