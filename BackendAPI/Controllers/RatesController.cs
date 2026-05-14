using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Services;
using BackendAPI.Models;
using BackendAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                var settings = await _context.UserSettings.FirstOrDefaultAsync(u => u.Id == 1);
                if (settings == null) return BadRequest("Settings not found.");

                string start = startDate ?? DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                string end = endDate ?? DateTime.Now.ToString("yyyy-MM-dd");

                var rates = await _exchangeRateService.GetTimeSeriesRatesAsync(
                    settings.BaseCurrency, 
                    settings.SelectedCurrencies, 
                    start, 
                    end, 
                    _context
                );

                var result = new CurrencyResultDto
                {
                    StrongestCurrency = _exchangeRateService.GetStrongestCurrency(rates),
                    WeakestCurrency = _exchangeRateService.GetWeakestCurrency(rates),
                    AverageRate = _exchangeRateService.GetAverageRate(rates),
                    TimeSeriesRates = rates
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _context.Logs.Add(new Log { Message = $"Controller Error: {ex.Message}", Timestamp = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("currencies")]
        public async Task<ActionResult<List<string>>> GetCurrencies()
        {
            try
            {
                var currencies = await _exchangeRateService.GetAvailableCurrenciesAsync();
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _context.Logs.Add(new Log { Message = $"Currency List Error: {ex.Message}", Timestamp = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                return StatusCode(500, "Internal server error");
            }
        }
    }
}