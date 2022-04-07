using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Anabasis.EntityFramework
{
    public static class DbContextOptionsBuilderExtension
    {
        public static DbContextOptionsBuilder With(this DbContextOptionsBuilder dbContextOptionsBuilder, ILoggerFactory? loggerFactory = null)
        {
            if (null != loggerFactory)
            {
                dbContextOptionsBuilder.UseLoggerFactory(loggerFactory);
            }

            return dbContextOptionsBuilder;
        }

        public static void BuildCustomizations(this ModelBuilder modelBuilder)
        {
        }

    }
}
