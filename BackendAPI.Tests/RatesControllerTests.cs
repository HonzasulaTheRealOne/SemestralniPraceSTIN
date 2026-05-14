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
            mockService.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());
            
            var controller = new RatesController(mockService.Object, GetInMemoryDbContext());
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_NoSettings_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()).Options;
            var emptyContext = new AppDbContext(options);

            var controller = new RatesController(new Mock<IExchangeRateService>().Object, emptyContext);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_NullDates_UsesDefaults()
        {
            var mockService = new Mock<IExchangeRateService>();
            mockService.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());

            var controller = new RatesController(mockService.Object, GetInMemoryDbContext());
            var result = await controller.AnalyzeRates(null, null); 

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_ThrowsException_Returns500AndLogs()
        {
            var mockService = new Mock<IExchangeRateService>();
            mockService.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ThrowsAsync(new System.Exception("API Error"));

            var context = GetInMemoryDbContext();
            var controller = new RatesController(mockService.Object, context);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetCurrencies_ReturnsOk()
        {
            var mockService = new Mock<IExchangeRateService>();
            mockService.Setup(s => s.GetAvailableCurrenciesAsync()).ReturnsAsync(new List<string> { "EUR" });
            
            var controller = new RatesController(mockService.Object, GetInMemoryDbContext());
            var result = await controller.GetCurrencies();

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetCurrencies_ThrowsException_Returns500()
        {
            var mockService = new Mock<IExchangeRateService>();
            mockService.Setup(s => s.GetAvailableCurrenciesAsync()).ThrowsAsync(new System.Exception("API Error"));
            
            var controller = new RatesController(mockService.Object, GetInMemoryDbContext());
            var result = await controller.GetCurrencies();

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}