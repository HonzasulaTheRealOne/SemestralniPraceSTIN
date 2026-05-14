using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using BackendAPI.Services;

namespace BackendAPI.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        public MockHttpMessageHandler(string response) { _response = response; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(_response) });
        }
    }

    public class ExchangeRateServiceTests
    {
        [Fact]
        public async Task GetAvailableCurrenciesAsync_ParsesJsonCorrectly()
        {
            var json = "{\"EUR\":\"Euro\",\"USD\":\"US Dollar\"}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client);

            var result = await service.GetAvailableCurrenciesAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains("EUR", result);
            Assert.Contains("USD", result);
        }

        [Fact]
        public async Task GetTimeSeriesRatesAsync_ParsesJsonCorrectly()
        {
            var json = "{\"rates\":{\"2026-05-01\":{\"USD\":1.08,\"CZK\":24.5}}}";
            var client = new HttpClient(new MockHttpMessageHandler(json));
            var service = new ExchangeRateService(client);

            var result = await service.GetTimeSeriesRatesAsync("EUR", "USD,CZK", "2026-05-01", "2026-05-01");

            Assert.True(result.ContainsKey("2026-05-01"));
            Assert.Equal(1.08m, result["2026-05-01"]["USD"]);
            Assert.Equal(24.5m, result["2026-05-01"]["CZK"]);
        }

        [Fact]
        public void GetStrongestCurrency_ReturnsCorrectCurrency()
        {
            var service = new ExchangeRateService(null!);
            var timeSeries = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2026-05-01", new Dictionary<string, decimal> { { "USD", 23.5m }, { "EUR", 25.0m } } },
                { "2026-05-02", new Dictionary<string, decimal> { { "USD", 24.0m }, { "GBP", 29.0m } } }
            };
            Assert.Equal("GBP", service.GetStrongestCurrency(timeSeries)); 
        }

        [Fact]
        public void GetStrongestCurrency_EmptyInput_ReturnsEmptyString()
        {
            var service = new ExchangeRateService(null!);
            Assert.Equal(string.Empty, service.GetStrongestCurrency(new Dictionary<string, Dictionary<string, decimal>>()));
        }

        [Fact]
        public void GetWeakestCurrency_ReturnsCorrectCurrency()
        {
            var service = new ExchangeRateService(null!);
            var timeSeries = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2026-05-01", new Dictionary<string, decimal> { { "USD", 23.5m }, { "EUR", 25.0m } } },
                { "2026-05-02", new Dictionary<string, decimal> { { "USD", 20.0m }, { "GBP", 29.0m } } }
            };
            Assert.Equal("USD", service.GetWeakestCurrency(timeSeries)); 
        }

        [Fact]
        public void GetWeakestCurrency_NullInput_ReturnsEmptyString()
        {
            var service = new ExchangeRateService(null!);
            Assert.Equal(string.Empty, service.GetWeakestCurrency(null!));
        }

        [Fact]
        public void GetAverageRate_CalculatesCorrectly()
        {
            var service = new ExchangeRateService(null!);
            var timeSeries = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2026-05-01", new Dictionary<string, decimal> { { "USD", 20.0m } } },
                { "2026-05-02", new Dictionary<string, decimal> { { "EUR", 25.0m } } }
            };
            Assert.Equal(22.5m, service.GetAverageRate(timeSeries));
        }

        [Fact]
        public void GetAverageRate_EmptyInput_ReturnsZero()
        {
            var service = new ExchangeRateService(null!);
            Assert.Equal(0m, service.GetAverageRate(null!));
        }
    }
}