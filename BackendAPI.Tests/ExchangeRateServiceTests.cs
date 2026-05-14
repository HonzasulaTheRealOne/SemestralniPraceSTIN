using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using BackendAPI.Services;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq;

namespace BackendAPI.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;
        public MockHttpMessageHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK) { _response = response; _statusCode = statusCode; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return Task.FromResult(new HttpResponseMessage { StatusCode = _statusCode, Content = new StringContent(_response) });
        }
    }

    public class ExchangeRateServiceTests
    {
        private AppDbContext GetDb() {
            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new AppDbContext(opt);
        }

        private IConfiguration GetMockConfig() {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["ExchangeRateApiKey"]).Returns("9df2dbeafc600550c8b34becd44556b2");
            return mockConfig.Object;
        }

        [Fact]
        public async Task GetTimeSeries_ApiSuccess_QuotesFormat_SavesToCache()
        {
            var json = "{\"success\":true,\"source\":\"USD\",\"quotes\":{\"USDUSD\":1.0,\"USDCZK\":23.5}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());
            var db = GetDb();

            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-14", "2026-05-14", db);

            Assert.Single(result);
            Assert.Equal(23.5m, result["2026-05-14"]["CZK"]);
            Assert.True(await db.CachedRates.AnyAsync(r => r.Currency == "CZK"));
        }

        [Fact]
        public async Task GetTimeSeries_ApiSuccess_RatesFormat_MissingSource_CalculatesCorrectly()
        {
            // Simulace alternativní odpovědi API (rates místo quotes, chybí source -> fallback na USD)
            var json = "{\"success\":true,\"rates\":{\"CZK\":23.5, \"EUR\":0.9}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());
            var db = GetDb();

            var result = await service.GetTimeSeriesRatesAsync("EUR", "CZK", "2026-05-14", "2026-05-14", db);

            Assert.Single(result);
            // 23.5 / 0.9 = 26.1111
            Assert.Equal(26.1111m, result["2026-05-14"]["CZK"]);
        }

        [Fact]
        public async Task GetTimeSeries_AllDaysInCache_SkipsApi()
        {
            var db = GetDb();
            db.CachedRates.Add(new CachedRate { Date = "2026-05-14", BaseCurrency = "USD", Currency = "CZK", Rate = 23.5m });
            await db.SaveChangesAsync();

            // Pokud by to zkusilo zavolat API, spadne to na 500
            var client = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.InternalServerError));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-14", "2026-05-14", db);

            Assert.Single(result);
            Assert.Equal(23.5m, result["2026-05-14"]["CZK"]);
        }

        [Fact]
        public async Task GetTimeSeries_ApiMissingRatesAndQuotes_SkipsDay()
        {
            var db = GetDb();
            var json = "{\"success\":true,\"source\":\"USD\"}"; // Chybí data
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-14", "2026-05-14", db);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTimeSeries_ApiFail_ReturnsFromCacheAndLogs()
        {
            var db = GetDb();
            db.CachedRates.Add(new CachedRate { Date = "2026-05-13", BaseCurrency = "USD", Currency = "CZK", Rate = 23.5m });
            await db.SaveChangesAsync();

            var errorJson = "{\"success\":false,\"error\":{\"info\":\"Limit reached\"}}";
            var client = new HttpClient(new MockHttpMessageHandler(errorJson));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-13", "2026-05-14", db);

            Assert.Equal(23.5m, result["2026-05-13"]["CZK"]);
            Assert.NotEmpty(db.Logs.Where(l => l.Message.Contains("Limit reached")));
        }

        [Fact]
        public async Task GetTimeSeries_NetworkError_CatchesExceptionAndLogs()
        {
            var db = GetDb();
            var client = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.InternalServerError));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-14", "2026-05-14", db);

            Assert.Empty(result);
            Assert.NotEmpty(db.Logs.Where(l => l.Level == "Error"));
        }

        [Fact]
        public void Calculations_WorkCorrectly()
        {
            var service = new ExchangeRateService(null!, GetMockConfig());
            var data = new Dictionary<string, Dictionary<string, decimal>> {
                { "d1", new Dictionary<string, decimal> { { "USD", 10m }, { "CZK", 20m } } },
                { "d2", new Dictionary<string, decimal> { { "USD", 12m }, { "CZK", 18m } } }
            };
            
            Assert.Equal("CZK", service.GetStrongestCurrency(data)); 
            Assert.Equal("USD", service.GetWeakestCurrency(data));   
            Assert.Equal(15m, service.GetAverageRate(data));         
        }

        [Fact]
        public void Calculations_EmptyData_ReturnsDefaults()
        {
            var service = new ExchangeRateService(null!, GetMockConfig());
            var emptyData = new Dictionary<string, Dictionary<string, decimal>>();
            
            Assert.Equal("", service.GetStrongestCurrency(emptyData));
            Assert.Equal("", service.GetWeakestCurrency(emptyData));
            Assert.Equal(0m, service.GetAverageRate(emptyData));
        }

        [Fact]
        public async Task GetAvailableCurrenciesAsync_Success_ReturnsList()
        {
            var json = "{\"success\":true,\"currencies\":{\"EUR\":\"Euro\",\"USD\":\"Dollar\"}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetAvailableCurrenciesAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains("EUR", result);
        }

        [Fact]
        public async Task GetAvailableCurrenciesAsync_ApiFails_ThrowsException()
        {
            var json = "{\"success\":false,\"error\":{\"info\":\"Plan restriction\"}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());

            var exception = await Assert.ThrowsAsync<Exception>(() => service.GetAvailableCurrenciesAsync());
            Assert.Contains("Plan restriction", exception.Message);
        }

        [Fact]
        public async Task GetAvailableCurrenciesAsync_MissingCurrencies_ThrowsException()
        {
            var json = "{\"success\":true,\"other\":\"data\"}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());

            var exception = await Assert.ThrowsAsync<Exception>(() => service.GetAvailableCurrenciesAsync());
            Assert.Contains("Neočekávaný formát", exception.Message);
        }
    }
}