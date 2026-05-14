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
            var rates = new Dictionary<string, decimal>
            {
                { "USD", 23.5m },
                { "EUR", 25.0m },
                { "GBP", 29.0m }
            };

            var result = service.GetStrongestCurrency(rates);

            Assert.Equal("GBP", result); 
        }

        [Fact]
        public void GetAverageRate_CalculatesCorrectly()
        {
            var service = new ExchangeRateService(null);
            var rates = new Dictionary<string, decimal>
            {
                { "USD", 20.0m },
                { "EUR", 25.0m }
            };

            var result = service.GetAverageRate(rates);

            Assert.Equal(22.5m, result);
        }
    }
}