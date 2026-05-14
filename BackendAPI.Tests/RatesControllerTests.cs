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
            var db = GetInMemoryDbContext();
            
            mockService.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppDbContext>()))
                       .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());
            
            var controller = new RatesController(mockService.Object, db);
            var result = await controller.AnalyzeRates("2026-01-01", "2026-01-02");

            Assert.IsType<OkObjectResult>(result.Result);
        }
    }
}