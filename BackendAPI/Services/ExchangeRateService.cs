using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

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
        private const string ApiBase = "https://api.exchangerate.host";

        public ExchangeRateService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<string>> GetAvailableCurrenciesAsync()
{
    try
    {
        var response = await _httpClient.GetAsync($"{ApiBase}/list");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(content);
        
        if (doc.RootElement.TryGetProperty("currencies", out var currenciesElement))
        {
            var currencies = new List<string>();
            foreach (var prop in currenciesElement.EnumerateObject())
            {
                currencies.Add(prop.Name);
            }
            return currencies;
        }
        else
        {

            throw new Exception($"API nevrátilo 'currencies'. Obsah: {content}");
        }
    }
    catch (Exception)
    {
        return new List<string> { "USD", "EUR", "CZK", "GBP", "CHF", "PLN" };
    }
}

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurr, string symbols, string start, string end, AppDbContext db)
        {
            try
            {
                var url = $"{ApiBase}/timeseries?base={baseCurr}&symbols={symbols}&start_date={start}&end_date={end}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(content);
                    var result = ParseRates(data);
                    await SaveRatesToCache(result, baseCurr, db);
                    return result;
                }
                throw new Exception("API Error");
            }
            catch (Exception ex)
            {
                db.Logs.Add(new Log { Message = ex.Message });
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