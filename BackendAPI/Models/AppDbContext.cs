using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Základní nastavení pro uživatele
            modelBuilder.Entity<UserSetting>().HasData(
                new UserSetting { Id = 1, BaseCurrency = "EUR", SelectedCurrencies = "USD,CZK,GBP" }
            );
        }
    }
}