using Microsoft.EntityFrameworkCore;
using System;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class TestDbContext : AnabasisDbContext
    {
        private static string _connectionString = $"Server=tcp:localhost,5434;Initial Catalog=tempdb;Persist Security Info=False;User ID=SA;Password=Your_password123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public TestDbContext() : base(new DbContextOptionsBuilder().UseSqlServer(_connectionString).Options)
        {
        }

        public TestDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreatingInternal(ModelBuilder modelBuilder)
        {

        }
    }
}
