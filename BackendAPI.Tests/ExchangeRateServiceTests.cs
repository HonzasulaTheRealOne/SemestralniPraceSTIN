using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using BackendAPI.Services;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;
        public MockHttpMessageHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _response = response;
            _statusCode = statusCode;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage { StatusCode = _statusCode, Content = new StringContent(_response) });
        }
    }

    public class ExchangeRateServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetTimeSeriesRatesAsync_ParsesJsonCorrectly()
        {
            var json = "{\"rates\":{\"2026-05-01\":{\"USD\":1.08}}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client);
            var db = GetInMemoryDbContext();

            var result = await service.GetTimeSeriesRatesAsync("EUR", "USD", "2026-05-01", "2026-05-01", db);

            Assert.True(result.ContainsKey("2026-05-01"));
            Assert.Equal(1.08m, result["2026-05-01"]["USD"]);
        }

        [Fact]
        public async Task GetTimeSeriesRatesAsync_ApiError_ReturnsCache()
        {
            var client = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.BadRequest));
            var service = new ExchangeRateService(client);
            var db = GetInMemoryDbContext();

            var result = await service.GetTimeSeriesRatesAsync("EUR", "USD", "2026-05-01", "2026-05-01", db);

            Assert.Empty(result);
            Assert.NotEmpty(db.Logs);
        }
    }
}