using System.Collections.Generic;

namespace BackendAPI.DTOs
{
    public class CurrencyResultDto
    {
        public Dictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
        public string StrongestCurrency { get; set; } = string.Empty;
        public string WeakestCurrency { get; set; } = string.Empty;
        public decimal AverageRate { get; set; }
    }
}