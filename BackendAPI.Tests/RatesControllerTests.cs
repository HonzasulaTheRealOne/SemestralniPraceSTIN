using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Services;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests
{
    public class RatesControllerTests
    {
        [Fact]
        public async Task Analyze_ReturnsOk()
        {
            var mock = new Mock<IExchangeRateService>();
            mock.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppDbContext>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());

            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("test").Options;
            var db = new AppDbContext(opt);
            db.UserSettings.Add(new UserSetting { Id = 1 });
            db.SaveChanges();

            var controller = new RatesController(mock.Object, db);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<OkObjectResult>(result.Result);
        }


        [Fact]
        public async Task AnalyzeRates_WhenSettingsMissing_ReturnsBadRequest()
        {
            var mock = new Mock<IExchangeRateService>();
            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(opt); 

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

            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(opt);
            db.UserSettings.Add(new UserSetting { Id = 1 });
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
            
            var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(opt);
            
            var controller = new RatesController(mock.Object, db);
            var result = await controller.GetCurrencies();

            Assert.IsType<OkObjectResult>(result.Result);
        }
    }
}