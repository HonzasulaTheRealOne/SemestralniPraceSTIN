using System;

namespace BackendAPI.Models
{
    public class UserSetting
    {
        public int Id { get; set; }
        public string BaseCurrency { get; set; } = "EUR";
        public string SelectedCurrencies { get; set; } = "USD,CZK,GBP";
        public string Language { get; set; } = "CZ";
    }

    public class Log
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Error";
        public string Message { get; set; } = string.Empty;
    }

    public class CachedRate
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string BaseCurrency { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}