using Microsoft.AspNetCore.Mvc;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

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
            var context = GetInMemoryDbContext();
            var controller = new SettingsController(context);

            var result = controller.GetSettings();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var settings = Assert.IsType<UserSetting>(okResult.Value);
            Assert.Equal(1, settings.Id);
        }

        [Fact]
        public void UpdateSettings_UpdatesExisting()
        {
            var context = GetInMemoryDbContext();
            context.UserSettings.Add(new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "CZK", Language = "CZ" });
            context.SaveChanges();
            
            var controller = new SettingsController(context);
            var newSettings = new UserSetting { Id = 1, BaseCurrency = "USD", SelectedCurrencies = "CZK", Language = "EN" };
            
            var result = controller.UpdateSettings(newSettings);

            Assert.IsType<OkResult>(result);
            var updated = context.UserSettings.Find(1);
            
            Assert.NotNull(updated);
            Assert.Equal("USD", updated.BaseCurrency);
            Assert.Equal("EN", updated.Language);
        }
    }
}