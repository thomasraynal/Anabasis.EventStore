using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    [TestFixture]
    public class TestConnection
    {
        [Test, Order(1)]
        public async Task ShouldWireUpEFConnection()
        {
            using var dbContext = new TestDbContext();

        }
    }
}
