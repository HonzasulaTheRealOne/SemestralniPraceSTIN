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
    [Authorize]
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
        public async Task<ActionResult<CurrencyResultDto>> AnalyzeRates([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            try
            {
                var settings = _context.UserSettings.FirstOrDefault(s => s.Id == 1);
                if (settings == null)
                {
                    return BadRequest("Nastavení nebylo nalezeno.");
                }

                if (string.IsNullOrEmpty(startDate)) startDate = DateTime.Now.AddDays(-10).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(endDate)) endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var timeSeries = await _exchangeRateService.GetTimeSeriesRatesAsync(settings.BaseCurrency, settings.SelectedCurrencies, startDate, endDate);

                var result = new CurrencyResultDto
                {
                    TimeSeriesRates = timeSeries,
                    StrongestCurrency = _exchangeRateService.GetStrongestCurrency(timeSeries),
                    WeakestCurrency = _exchangeRateService.GetWeakestCurrency(timeSeries),
                    AverageRate = _exchangeRateService.GetAverageRate(timeSeries)
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