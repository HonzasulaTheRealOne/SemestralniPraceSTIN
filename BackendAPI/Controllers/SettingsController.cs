using System.Linq;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public ActionResult<UserSetting> GetSettings()
        {
            var settings = _context.UserSettings.FirstOrDefault(u => u.Id == 1);
            if (settings == null)
            {
                settings = new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD,CZK", Language = "CZ" };
                _context.UserSettings.Add(settings);
                _context.SaveChanges();
            }
            return Ok(settings);
        }

        [HttpPost]
        public ActionResult UpdateSettings([FromBody] UserSetting settings)
        {
            var existing = _context.UserSettings.FirstOrDefault(u => u.Id == 1);
            if (existing != null)
            {
                if (existing.BaseCurrency != settings.BaseCurrency)
                {
                    _context.Logs.Add(new Log { Level = "Info", Message = $"Základní měna změněna z {existing.BaseCurrency} na {settings.BaseCurrency}" });
                }

                existing.BaseCurrency = settings.BaseCurrency;
                existing.SelectedCurrencies = settings.SelectedCurrencies;
                existing.Language = settings.Language;
                _context.SaveChanges();
            }
            return Ok();
        }
    }
}