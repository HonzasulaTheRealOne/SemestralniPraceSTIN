using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Services;
using BackendAPI.Models;
using BackendAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests
{
    public class RatesControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD", Language = "CZ" });
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task AnalyzeRates_ReturnsOk_WithValidData()
        {
            var mockService = new Mock<IExchangeRateService>();
            var fakeTimeSeries = new Dictionary<string, Dictionary<string, decimal>>(); 
            mockService.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(fakeTimeSeries);
            
            var context = GetInMemoryDbContext();
            var controller = new RatesController(mockService.Object, context);

            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<CurrencyResultDto>(okResult.Value);
        }

        [Fact]
        public async Task GetCurrencies_ReturnsOk()
        {
            var mockService = new Mock<IExchangeRateService>();
            mockService.Setup(s => s.GetAvailableCurrenciesAsync())
                       .ReturnsAsync(new List<string> { "EUR", "USD" });
            
            var context = GetInMemoryDbContext();
            var controller = new RatesController(mockService.Object, context);

            var result = await controller.GetCurrencies();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var currencies = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, currencies.Count);
        }
    }
}