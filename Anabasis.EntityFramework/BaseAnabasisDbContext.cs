using Microsoft.EntityFrameworkCore;
using System;

namespace Anabasis.EntityFramework
{

    public abstract class BaseAnabasisDbContext : DbContext
    {
        protected BaseAnabasisDbContext(DbContextOptionsBuilder dbContextOptionsBuilder) : base(dbContextOptionsBuilder.Options)
        {
        }

        public BaseAnabasisDbContext(DbContextOptions options) : base(new DbContextOptionsBuilder(options).Options)
        { 
        }

        protected override sealed void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingInternal(modelBuilder);
            modelBuilder.BuildCustomizations();
        }

        protected abstract void OnModelCreatingInternal(ModelBuilder modelBuilder);
    }
}
