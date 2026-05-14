using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BackendAPI.Controllers;
using BackendAPI.Services;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace BackendAPI.Tests
{
    public class AuthControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Login_Success_LogsInfo()
        {
            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.ValidateCredentials("admin", "admin")).Returns(true);
            mockAuth.Setup(s => s.GenerateJwtToken("admin")).Returns("fake_token");

            var db = GetInMemoryDbContext();
            var controller = new AuthController(mockAuth.Object, db);

            var result = await controller.Login(new LoginDto { Username = "admin", Password = "admin" });

            Assert.IsType<OkObjectResult>(result);
            var log = db.Logs.FirstOrDefault(l => l.Message.Contains("úspěšně přihlásil"));
            Assert.NotNull(log);
            Assert.Equal("Info", log.Level);
        }

        [Fact]
        public async Task Logout_LogsInfo()
        {
            var mockAuth = new Mock<IAuthService>();
            var db = GetInMemoryDbContext();
            var controller = new AuthController(mockAuth.Object, db);

            var result = await controller.Logout(new LoginDto { Username = "admin" });

            Assert.IsType<OkResult>(result);
            var log = db.Logs.FirstOrDefault(l => l.Message.Contains("odhlásil"));
            Assert.NotNull(log);
            Assert.Equal("Info", log.Level);
        }
    }
}