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
            mockConfig.Setup(c => c["ExchangeRateApiKey"]).Returns("test_key");
            return mockConfig.Object;
        }

        [Fact]
        public async Task GetTimeSeries_ApiSuccess_SavesToCache()
        {
            var json = "{\"rates\":{\"2026-01-01\":{\"USD\":1.1}}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());
            var db = GetDb();

            var result = await service.GetTimeSeriesRatesAsync("EUR", "USD", "2026-01-01", "2026-01-01", db);

            Assert.Single(result);
            Assert.Equal(1.1m, result["2026-01-01"]["USD"]);
            Assert.True(await db.CachedRates.AnyAsync(r => r.Currency == "USD"));
        }

        [Fact]
        public async Task GetTimeSeries_ApiFail_ReturnsFromCacheAndLogs()
        {
            var db = GetDb();
            db.CachedRates.Add(new CachedRate { Date = "2026-01-01", BaseCurrency = "EUR", Currency = "USD", Rate = 1.2m });
            await db.SaveChangesAsync();

            var client = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.InternalServerError));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetTimeSeriesRatesAsync("EUR", "USD", "2026-01-01", "2026-01-01", db);

            Assert.Equal(1.2m, result["2026-01-01"]["USD"]);
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
            var json = "{\"currencies\":{\"EUR\":\"Euro\",\"USD\":\"Dollar\"}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client, GetMockConfig());

            var result = await service.GetAvailableCurrenciesAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains("EUR", result);
        }
    }
}