using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.DTOs;
using BackendAPI.Models;
using BackendAPI.Services;
using System.Linq;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ochrání endpoint tak, že sem může jen přihlášený uživatel s tokenem
    public class RatesController : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService;
        private readonly AppDbContext _context;

        public RatesController(IExchangeRateService exchangeRateService, AppDbContext context)
        {
            _exchangeRateService = exchangeRateService;
            _context = context;
        }

        [HttpGet("analyze")]
        public async Task<ActionResult<CurrencyResultDto>> AnalyzeRates()
        {
            try
            {
                var settings = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
                if (settings == null)
                {
                    return BadRequest("Nastavení nebylo nalezeno.");
                }

                var rates = await _exchangeRateService.GetRatesAsync(settings.BaseCurrency, settings.SelectedCurrencies);

                var result = new CurrencyResultDto
                {
                    Rates = rates,
                    StrongestCurrency = _exchangeRateService.GetStrongestCurrency(rates),
                    WeakestCurrency = _exchangeRateService.GetWeakestCurrency(rates),
                    AverageRate = _exchangeRateService.GetAverageRate(rates)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _context.Logs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = "ERROR",
                    Message = ex.Message
                });
                await _context.SaveChangesAsync();

                return StatusCode(500, "Chyba při komunikaci s API: " + ex.Message);
            }
        }
    }
}