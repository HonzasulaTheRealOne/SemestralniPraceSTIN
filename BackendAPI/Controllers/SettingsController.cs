using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;
using System.Linq;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<UserSetting> GetSettings()
        {
            var settings = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null)
            {
                settings = new UserSetting { Id = 1 };
                _context.UserSettings.Add(settings);
                _context.SaveChanges();
            }
            return Ok(settings);
        }

        [HttpPost]
        public ActionResult UpdateSettings(UserSetting settings)
        {
            var existing = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
            if (existing != null)
            {
                existing.BaseCurrency = settings.BaseCurrency;
                existing.SelectedCurrencies = settings.SelectedCurrencies;
                existing.Language = settings.Language;
                _context.SaveChanges();
            }
            return Ok();
        }
    }
}