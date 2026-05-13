using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Models;

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
        public IActionResult GetSettings()
        {
            var settings = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null)
            {
                return NotFound();
            }
            return Ok(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] UserSetting updatedSettings)
        {
            var settings = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null)
            {
                updatedSettings.Id = 1;
                _context.UserSettings.Add(updatedSettings);
            }
            else
            {
                settings.BaseCurrency = updatedSettings.BaseCurrency;
                settings.SelectedCurrencies = updatedSettings.SelectedCurrencies;
            }

            await _context.SaveChangesAsync();
            return Ok(settings);
        }
    }
}