namespace BackendAPI.Models
{
    public class UserSetting
    {
        public int Id { get; set; }
        public string BaseCurrency { get; set; } = "EUR";
        public string SelectedCurrencies { get; set; } = "USD,CZK,GBP";
    }
}