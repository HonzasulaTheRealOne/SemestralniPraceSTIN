using Microsoft.AspNetCore.Mvc;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BackendAPI.Tests
{
    public class SettingsControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void GetSettings_CreatesDefault_IfNoneExists()
        {
            var controller = new SettingsController(GetInMemoryDbContext());
            var result = controller.GetSettings();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserSetting>(okResult.Value);
        }

        [Fact]
        public void GetSettings_ReturnsExisting_IfExists()
        {
            var context = GetInMemoryDbContext();
            context.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "PLN", Language = "EN" });
            context.SaveChanges();

            var controller = new SettingsController(context);
            var result = controller.GetSettings();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var settings = Assert.IsType<UserSetting>(okResult.Value);
            
            Assert.Equal("PLN", settings.BaseCurrency);
        }

        [Fact]
        public void UpdateSettings_UpdatesExisting_AndLogsChange()
        {
            var context = GetInMemoryDbContext();
            context.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR" });
            context.SaveChanges();
            
            var controller = new SettingsController(context);
            var result = controller.UpdateSettings(new UserSetting { Id = 1, BaseCurrency = "USD" });

            Assert.IsType<OkResult>(result);
            Assert.Equal("USD", context.UserSettings.Find(1)?.BaseCurrency);
            
            var log = context.Logs.FirstOrDefault();
            Assert.NotNull(log);
            Assert.Contains("změněna z EUR na USD", log.Message);
        }

        [Fact]
        public void UpdateSettings_UpdatesExisting_NoLogIfBaseSame()
        {
            var context = GetInMemoryDbContext();
            context.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "CZK" });
            context.SaveChanges();
            
            var controller = new SettingsController(context);
            // Měníme jen vybrané měny, BaseCurrency zůstává EUR
            var result = controller.UpdateSettings(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD" });

            Assert.IsType<OkResult>(result);
            Assert.Empty(context.Logs);
        }

        [Fact]
        public void UpdateSettings_DoesNothing_IfNoSettingsExist()
        {
            var context = GetInMemoryDbContext();
            var controller = new SettingsController(context);
            
            var result = controller.UpdateSettings(new UserSetting { Id = 1, BaseCurrency = "USD" });

            Assert.IsType<OkResult>(result);
            Assert.Empty(context.UserSettings);
        }
    }
}