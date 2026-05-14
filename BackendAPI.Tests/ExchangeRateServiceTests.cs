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
        public async Task GetTimeSeries_ApiSuccess_SavesToCache()
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
        public async Task GetTimeSeries_ApiFail_ReturnsFromCacheAndLogs()
        {
            var db = GetDb();
            // 1. Předvyplníme cache jen pro 13.5.
            db.CachedRates.Add(new CachedRate { Date = "2026-05-13", BaseCurrency = "USD", Currency = "CZK", Rate = 23.5m });
            await db.SaveChangesAsync();

            // 2. Připravíme padající API (pro dny, které v cache nejsou)
            var errorJson = "{\"success\":false,\"error\":{\"info\":\"Limit reached\"}}";
            var client = new HttpClient(new MockHttpMessageHandler(errorJson));
            var service = new ExchangeRateService(client, GetMockConfig());

            // 3. Dotaz na DVA dny: 13.5. (vezme z cache) a 14.5. (zavolá API a selže)
            var result = await service.GetTimeSeriesRatesAsync("USD", "CZK", "2026-05-13", "2026-05-14", db);

            // Zkontroluje, že se vrátil výsledek z cache pro 13.5.
            Assert.Equal(23.5m, result["2026-05-13"]["CZK"]);
            // Zkontroluje, že se selhání API pro 14.5. zapsalo do logu
            Assert.NotEmpty(db.Logs);
        }

        [Fact]
        public void Calculations_WorkCorrectly()
        {
            var service = new ExchangeRateService(null!, GetMockConfig());
            var data = new Dictionary<string, Dictionary<string, decimal>> {
                { "d1", new Dictionary<string, decimal> { { "USD", 10m }, { "CZK", 20m } } }
            };
            Assert.Equal("CZK", service.GetStrongestCurrency(data));
            Assert.Equal("USD", service.GetWeakestCurrency(data));
            Assert.Equal(15m, service.GetAverageRate(data));
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
    }
}