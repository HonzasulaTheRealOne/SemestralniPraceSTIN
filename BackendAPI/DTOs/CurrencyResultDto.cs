using System.Collections.Generic;

namespace BackendAPI.DTOs
{
    public class CurrencyResultDto
    {
        public Dictionary<string, Dictionary<string, decimal>>? TimeSeriesRates { get; set; }
        public string? StrongestCurrency { get; set; }
        public string? WeakestCurrency { get; set; }
        public decimal AverageRate { get; set; }
    }
}