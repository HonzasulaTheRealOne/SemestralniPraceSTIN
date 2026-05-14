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
        private const string ApiBase = "http://api.exchangerate.host"; 

        public ExchangeRateService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            // Tvůj aktuální klíč
            _apiKey = config["ExchangeRateApiKey"] ?? "9df2dbeafc600550c8b34becd44556b2";
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync()
        {
            var response = await _httpClient.GetAsync($"{ApiBase}/list?access_key={_apiKey}");
            var content = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(content);
            
            // Detailní zachycení chyby API
            if (doc.RootElement.TryGetProperty("success", out var successEl) && successEl.GetBoolean() == false)
            {
                var error = doc.RootElement.GetProperty("error").GetProperty("info").GetString();
                throw new Exception($"API Error: {error}");
            }

            if (doc.RootElement.TryGetProperty("currencies", out var currenciesElement))
            {
                var currencies = new List<string>();
                foreach (var prop in currenciesElement.EnumerateObject()) currencies.Add(prop.Name);
                return currencies;
            }
            
            throw new Exception($"Neočekávaný formát z API: {content}");
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurr, string symbols, string start, string end, AppDbContext db)
        {
            try
            {
                // TRIK PRO FREE PLÁN: Záměrně neposíláme parametr &base=. API tak použije defaultní EUR.
                // Do symbols ale přidáme i naši zvolenou základní měnu, abychom znali její kurz vůči EUR a mohli provést přepočet.
                var symbolsWithBase = $"{symbols},{baseCurr}";
                var url = $"{ApiBase}/timeseries?access_key={_apiKey}&symbols={symbolsWithBase}&start_date={start}&end_date={end}";
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                using var data = JsonDocument.Parse(content);

                if (data.RootElement.TryGetProperty("success", out var successEl) && successEl.GetBoolean() == false)
                {
                    var error = data.RootElement.GetProperty("error").GetProperty("info").GetString();
                    throw new Exception($"API Error: {error}");
                }

                if (data.RootElement.TryGetProperty("rates", out var ratesProp))
                {
                    var parsedRates = new Dictionary<string, Dictionary<string, decimal>>();
                    var requestedSymbols = symbols.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                    foreach (var dateProp in ratesProp.EnumerateObject())
                    {
                        var dayRates = new Dictionary<string, decimal>();
                        var eurBasedRates = new Dictionary<string, decimal>();

                        foreach (var currProp in dateProp.Value.EnumerateObject())
                        {
                            eurBasedRates.Add(currProp.Name, currProp.Value.GetDecimal());
                        }

                        // MATEMATICKÝ PŘEPOČET NA LOKÁLNÍ STRANĚ:
                        // Zjistíme kurz požadované základní měny vůči EUR.
                        decimal baseRateToEur = baseCurr == "EUR" ? 1m : (eurBasedRates.ContainsKey(baseCurr) ? eurBasedRates[baseCurr] : 1m);

                        // Přepočítáme všechny kurzy na zvolenou základní měnu.
                        foreach (var symbol in requestedSymbols)
                        {
                            if (eurBasedRates.ContainsKey(symbol) && baseRateToEur > 0)
                            {
                                var calculatedRate = eurBasedRates[symbol] / baseRateToEur;
                                dayRates.Add(symbol, Math.Round(calculatedRate, 4));
                            }
                        }
                        parsedRates.Add(dateProp.Name, dayRates);
                    }

                    // Uložíme reálná převedená data do databáze (Cache)
                    await SaveRatesToCache(parsedRates, baseCurr, db);
                    return parsedRates;
                }
                throw new Exception($"Chybí pole rates. Obsah: {content}");
            }
            catch (Exception ex)
            {
                // Zapíšeme reálný důvod selhání do UI Logů
                db.Logs.Add(new Log { Level = "Error", Message = $"TimeSeries: {ex.Message}" });
                await db.SaveChangesAsync();
                
                // Při výpadku vrátíme to, co se do Cache uložilo minule (žádná falešná data)
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

        public string GetStrongestCurrency(Dictionary<string, Dictionary<string, decimal>> data) => data.SelectMany(d => d.Value).OrderByDescending(v => v.Value).FirstOrDefault().Key ?? "";
        public string GetWeakestCurrency(Dictionary<string, Dictionary<string, decimal>> data) => data.SelectMany(d => d.Value).OrderBy(v => v.Value).FirstOrDefault().Key ?? "";
        public decimal GetAverageRate(Dictionary<string, Dictionary<string, decimal>> data) {
            var allValues = data.SelectMany(d => d.Value.Values).ToList();
            return allValues.Any() ? allValues.Average() : 0;
        }
    }
}