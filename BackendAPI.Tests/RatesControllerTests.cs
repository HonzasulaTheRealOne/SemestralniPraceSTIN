using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Services;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BackendAPI.Tests
{
    public class RatesControllerTests
    {
        private AppDbContext GetDb()
        {
            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new AppDbContext(opt);
        }

        [Fact]
        public async Task Analyze_ReturnsOk()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppDbContext>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());

            var db = GetDb();
            db.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD" });
            db.SaveChanges();

            var controller = new RatesController(mock.Object, db);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_NullDates_UsesDefaultDates()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppDbContext>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());

            var db = GetDb();
            db.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD" });
            db.SaveChanges();

            var controller = new RatesController(mock.Object, db);
            var result = await controller.AnalyzeRates(null, null);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_WhenSettingsMissing_ReturnsBadRequest()
        {
            var mock = new Mock<IExchangeRateService>();
            var db = GetDb(); 

            var controller = new RatesController(mock.Object, db);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeRates_WhenServiceFails_Returns500AndLogs()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppDbContext>()))
                .ThrowsAsync(new System.Exception("Kritická chyba API"));

            var db = GetDb();
            db.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD" });
            db.SaveChanges();

            var controller = new RatesController(mock.Object, db);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.NotEmpty(db.Logs);
        }

        [Fact]
        public async Task GetCurrencies_ReturnsOk()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetAvailableCurrenciesAsync()).ReturnsAsync(new List<string> { "CZK", "EUR" });
            
            var db = GetDb();
            var controller = new RatesController(mock.Object, db);
            var result = await controller.GetCurrencies();

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetCurrencies_WhenServiceFails_Returns500AndLogs()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetAvailableCurrenciesAsync()).ThrowsAsync(new System.Exception("API je nedostupné"));
            
            var db = GetDb();
            var controller = new RatesController(mock.Object, db);
            var result = await controller.GetCurrencies();

            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            
            var log = db.Logs.FirstOrDefault();
            Assert.NotNull(log);
            Assert.Contains("Currency List Error", log.Message);
        }
    }
}