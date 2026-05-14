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
        Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurrency, string symbols, string startDate, string endDate);
        string GetStrongestCurrency(Dictionary<string, Dictionary<string, decimal>> timeSeries);
        string GetWeakestCurrency(Dictionary<string, Dictionary<string, decimal>> timeSeries);
        decimal GetAverageRate(Dictionary<string, Dictionary<string, decimal>> timeSeries);
    }

    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;

        public ExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurrency, string symbols, string startDate, string endDate)
        {
            var url = $"https://api.frankfurter.app/{startDate}..{endDate}?from={baseCurrency}&to={symbols}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);

            var ratesElement = document.RootElement.GetProperty("rates");
            var result = new Dictionary<string, Dictionary<string, decimal>>();

            foreach (var dateProperty in ratesElement.EnumerateObject())
            {
                var dateRates = new Dictionary<string, decimal>();
                foreach (var currencyProperty in dateProperty.Value.EnumerateObject())
                {
                    dateRates.Add(currencyProperty.Name, currencyProperty.Value.GetDecimal());
                }
                result.Add(dateProperty.Name, dateRates);
            }

            return result;
        }

        public string GetStrongestCurrency(Dictionary<string, Dictionary<string, decimal>> timeSeries)
        {
            if (timeSeries == null || !timeSeries.Any()) return string.Empty;
            var allRates = timeSeries.SelectMany(d => d.Value);
            return allRates.OrderByDescending(r => r.Value).First().Key;
        }

        public string GetWeakestCurrency(Dictionary<string, Dictionary<string, decimal>> timeSeries)
        {
            if (timeSeries == null || !timeSeries.Any()) return string.Empty;
            var allRates = timeSeries.SelectMany(d => d.Value);
            return allRates.OrderBy(r => r.Value).First().Key;
        }

        public decimal GetAverageRate(Dictionary<string, Dictionary<string, decimal>> timeSeries)
        {
            if (timeSeries == null || !timeSeries.Any()) return 0;
            var allRates = timeSeries.SelectMany(d => d.Value);
            return allRates.Average(r => r.Value);
        }
    }
}