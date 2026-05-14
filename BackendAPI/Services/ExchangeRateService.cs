using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BackendAPI.Services
{
    public interface IExchangeRateService
    {
        Task<List<string>> GetAvailableCurrenciesAsync();
        Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurr, string symbols, string start, string end, AppDbContext db);
        string GetStrongestCurrency(Dictionary<string, Dictionary<string, decimal>> data);
        string GetWeakestCurrency(Dictionary<string, Dictionary<string, decimal>> data);
        decimal GetAverageRate(Dictionary<string, Dictionary<string, decimal>> data);
    }

    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        // U free plánů exchangerate často blokuje HTTPS, proto raději http://
        private const string ApiBase = "http://api.exchangerate.host"; 

        public ExchangeRateService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            // Přečteme klíč z konfigurace, jako zálohu použijeme ten tvůj
            _apiKey = config["ExchangeRateApiKey"] ?? "9df2dbeafc600550c8b34becd44556b2";
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync()
        {
            try
            {
                // PŘIDÁNO: ?access_key=
                var response = await _httpClient.GetAsync($"{ApiBase}/list?access_key={_apiKey}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(content);
                // Bezpečné parsování - pokud API pošle chybu o limitech, nespadneme
                if (doc.RootElement.TryGetProperty("currencies", out var currenciesElement))
                {
                    var currencies = new List<string>();
                    foreach (var prop in currenciesElement.EnumerateObject()) currencies.Add(prop.Name);
                    return currencies;
                }
                throw new Exception($"API nevrátilo 'currencies'. Obsah: {content}");
            }
            catch (Exception)
            {
                // FALLBACK: Pokud API selže, vrátíme aspoň toto, aby UI nezamrzlo
                return new List<string> { "USD", "EUR", "CZK", "GBP", "CHF", "PLN" };
            }
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurr, string symbols, string start, string end, AppDbContext db)
        {
            try
            {
                // PŘIDÁNO: ?access_key=
                var url = $"{ApiBase}/timeseries?access_key={_apiKey}&base={baseCurr}&symbols={symbols}&start_date={start}&end_date={end}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var data = JsonDocument.Parse(content);
                    if (data.RootElement.TryGetProperty("rates", out var ratesProp))
                    {
                        var result = ParseRates(data);
                        await SaveRatesToCache(result, baseCurr, db);
                        return result;
                    }
                    throw new Exception($"API nevrátilo 'rates'. Obsah: {content}");
                }
                throw new Exception($"API Error {response.StatusCode}. Obsah: {content}");
            }
            catch (Exception ex)
            {
                db.Logs.Add(new Log { Level = "Error", Message = $"API Error: {ex.Message}" });
                await db.SaveChangesAsync();
                return await GetRatesFromCache(baseCurr, symbols.Split(','), db);
            }
        }

        private async Task SaveRatesToCache(Dictionary<string, Dictionary<string, decimal>> data, string baseCurr, AppDbContext db)
        {
            foreach (var dateEntry in data)
            {
                foreach (var rateEntry in dateEntry.Value)
                {
                    var exists = await db.CachedRates.AnyAsync(r => r.Date == dateEntry.Key && r.Currency == rateEntry.Key && r.BaseCurrency == baseCurr);
                    if (!exists) db.CachedRates.Add(new CachedRate { Date = dateEntry.Key, BaseCurrency = baseCurr, Currency = rateEntry.Key, Rate = rateEntry.Value });
                }
            }
            await db.SaveChangesAsync();
        }

        private async Task<Dictionary<string, Dictionary<string, decimal>>> GetRatesFromCache(string baseCurr, string[] symbols, AppDbContext db)
        {
            var cached = await db.CachedRates.Where(r => r.BaseCurrency == baseCurr && symbols.Contains(r.Currency)).ToListAsync();
            return cached.GroupBy(r => r.Date).ToDictionary(g => g.Key, g => g.ToDictionary(r => r.Currency, r => r.Rate));
        }

        private Dictionary<string, Dictionary<string, decimal>> ParseRates(JsonDocument data)
        {
            var result = new Dictionary<string, Dictionary<string, decimal>>();
            if (!data.RootElement.TryGetProperty("rates", out var ratesProp)) return result;
            foreach (var dateProp in ratesProp.EnumerateObject())
            {
                var dayRates = new Dictionary<string, decimal>();
                foreach (var currProp in dateProp.Value.EnumerateObject()) dayRates.Add(currProp.Name, currProp.Value.GetDecimal());
                result.Add(dateProp.Name, dayRates);
            }
            return result;
        }

        public string GetStrongestCurrency(Dictionary<string, Dictionary<string, decimal>> data) => data.SelectMany(d => d.Value).OrderByDescending(v => v.Value).FirstOrDefault().Key ?? "";
        public string GetWeakestCurrency(Dictionary<string, Dictionary<string, decimal>> data) => data.SelectMany(d => d.Value).OrderBy(v => v.Value).FirstOrDefault().Key ?? "";
        public decimal GetAverageRate(Dictionary<string, Dictionary<string, decimal>> data) {
            var allValues = data.SelectMany(d => d.Value.Values).ToList();
            return allValues.Any() ? allValues.Average() : 0;
        }
    }
}