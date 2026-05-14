using System.Collections.Generic;
using Xunit;
using BackendAPI.Services;

namespace BackendAPI.Tests
{
    public class ExchangeRateServiceTests
    {
        [Fact]
        public void GetStrongestCurrency_ReturnsCorrectCurrency()
        {
            var service = new ExchangeRateService(null);
            var timeSeries = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2026-05-01", new Dictionary<string, decimal> { { "USD", 23.5m }, { "EUR", 25.0m } } },
                { "2026-05-02", new Dictionary<string, decimal> { { "USD", 24.0m }, { "GBP", 29.0m } } }
            };

            var result = service.GetStrongestCurrency(timeSeries);
            Assert.Equal("GBP", result); 
        }

        [Fact]
        public void GetAverageRate_CalculatesCorrectly()
        {
            var service = new ExchangeRateService(null);
            var timeSeries = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2026-05-01", new Dictionary<string, decimal> { { "USD", 20.0m } } },
                { "2026-05-02", new Dictionary<string, decimal> { { "EUR", 25.0m } } }
            };

            var result = service.GetAverageRate(timeSeries);
            Assert.Equal(22.5m, result);
        }
    }
}