using Microsoft.EntityFrameworkCore;
using System;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class TestDbContext : AnabasisDbContext
    {
        private static readonly string _connectionString = $"Server=tcp:localhost,5434;Initial Catalog=tempdb;Persist Security Info=False;User ID=SA;Password=Your_password123;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;";

        public TestDbContext() : base(new DbContextOptionsBuilder().UseSqlServer(_connectionString).Options)
        {
        }

        public TestDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreatingInternal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Currency>().HasKey(currency => new { currency.Name, currency.Code });

            modelBuilder.Entity<Currency>().HasData(new Currency("United States dollar", "USD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Euro", "EUR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Japanese yen", "JPY"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Pound sterling", "GBP"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Australian dollar", "AUD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Canadian dollar", "CAD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Swiss franc", "CHF"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Renminbi", "CNY"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Hong Kong dollar", "HKD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("New Zealand dollar", "NZD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Swedish krona", "SEK"));
            modelBuilder.Entity<Currency>().HasData(new Currency("South Korean won", "KRW"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Singapore dollar", "SGD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Norwegian krone", "NOK"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Mexican peso", "MXN"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Indian rupee", "INR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Russian ruble", "RUB"));
            modelBuilder.Entity<Currency>().HasData(new Currency("South African rand", "ZAR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Turkish lira", "TRY"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Brazilian real", "BRL"));
            modelBuilder.Entity<Currency>().HasData(new Currency("New Taiwan dollar", "TWD"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Danish krone", "DKK"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Polish złoty", "PLN"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Thai baht", "THB"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Indonesian rupiah", "IDR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Hungarian forint", "HUF"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Czech koruna", "CZK"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Israeli new shekel", "ILS"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Chilean peso", "CLP"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Philippine peso", "PHP"));
            modelBuilder.Entity<Currency>().HasData(new Currency("UAE dirham", "AED"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Colombian peso", "COP"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Saudi riyal", "SAR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Malaysian ringgit", "MYR"));
            modelBuilder.Entity<Currency>().HasData(new Currency("Romanian leu", "RON"));

        }

        public DbSet<Currency> Currencies { get; set; }
    }
}
