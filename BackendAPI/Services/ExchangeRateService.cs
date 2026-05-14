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
            _apiKey = config["ExchangeRateApiKey"] ?? "7983b9057aad82a308abc5a9f2f7d0a8";
        }

        public async Task<List<string>> GetAvailableCurrenciesAsync()
        {
            var response = await _httpClient.GetAsync($"{ApiBase}/list?access_key={_apiKey}");
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            
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
            throw new Exception($"Neočekávaný formát: {content}");
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(string baseCurr, string symbols, string start, string end, AppDbContext db)
        {
            var result = new Dictionary<string, Dictionary<string, decimal>>();
            var startDate = DateTime.Parse(start);
            var endDate = DateTime.Parse(end);
            var requestedSymbols = symbols.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyy-MM-dd");
                
                var cachedForDay = await db.CachedRates.Where(r => r.BaseCurrency == baseCurr && r.Date == dateStr && requestedSymbols.Contains(r.Currency)).ToListAsync();

                if (cachedForDay.Count >= requestedSymbols.Count && requestedSymbols.Count > 0)
                {
                    result.Add(dateStr, cachedForDay.ToDictionary(r => r.Currency, r => r.Rate));
                    continue;
                }

                try
                {
                    var symbolsWithBase = $"{symbols},{baseCurr}";
                    var url = $"{ApiBase}/historical?access_key={_apiKey}&date={dateStr}&symbols={symbolsWithBase}";
                    
                    var response = await _httpClient.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    using var data = JsonDocument.Parse(content);

                    if (data.RootElement.TryGetProperty("success", out var successEl) && successEl.GetBoolean() == false)
                    {
                        var error = data.RootElement.GetProperty("error").GetProperty("info").GetString();
                        db.Logs.Add(new Log { Level = "Warning", Message = $"API Error pro {dateStr}: {error}" });
                        await db.SaveChangesAsync();
                        continue;
                    }

                    JsonElement ratesObj = data.RootElement.TryGetProperty("rates", out var r) ? r : 
                                           data.RootElement.TryGetProperty("quotes", out var q) ? q : default;

                    if (ratesObj.ValueKind == JsonValueKind.Undefined) continue;

                    string sourceCurrency = data.RootElement.TryGetProperty("source", out var src) ? src.GetString() : "USD";

                    var dayRates = new Dictionary<string, decimal>();
                    var rawRates = new Dictionary<string, decimal>();

                    foreach (var prop in ratesObj.EnumerateObject())
                    {
                        string currencyCode = prop.Name.Length == 6 && prop.Name.StartsWith(sourceCurrency) ? prop.Name.Substring(3) : prop.Name;
                        rawRates.Add(currencyCode, prop.Value.GetDecimal());
                    }

                    decimal baseRateToSource = baseCurr == sourceCurrency ? 1m : (rawRates.ContainsKey(baseCurr) ? rawRates[baseCurr] : 1m);

                    foreach (var symbol in requestedSymbols)
                    {
                        if (rawRates.ContainsKey(symbol) && baseRateToSource > 0)
                        {
                            var calculatedRate = rawRates[symbol] / baseRateToSource;
                            var roundedRate = Math.Round(calculatedRate, 4);
                            dayRates.Add(symbol, roundedRate);
                            
                            db.CachedRates.Add(new CachedRate { Date = dateStr, BaseCurrency = baseCurr, Currency = symbol, Rate = roundedRate });
                        }
                    }

                    result.Add(dateStr, dayRates);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    db.Logs.Add(new Log { Level = "Error", Message = $"Chyba při stahování {dateStr}: {ex.Message}" });
                    await db.SaveChangesAsync();
                }
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