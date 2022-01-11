using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    [TestFixture]
    public class TestConnection
    {
        [Test]
        public async Task ShouldWireUpEFConnection()
        {
            using var dbContext = new TestDbContext();

            var currencies = await dbContext.Currencies.ToArrayAsync();

            Assert.IsNotEmpty(currencies);

        }
    }
}
