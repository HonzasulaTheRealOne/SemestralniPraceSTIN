using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<Log> Logs { get; set; } // Opraveno z LogEntry na Log
        public DbSet<CachedRate> CachedRates { get; set; } // Přidáno

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Základní nastavení pro uživatele při prvním spuštění
            modelBuilder.Entity<UserSetting>().HasData(
                new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD,CZK,GBP", Language = "CZ" }
            );
        }
    }
}