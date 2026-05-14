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
    }
}