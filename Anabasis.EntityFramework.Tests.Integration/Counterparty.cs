using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class Counterparty
    {
        private Counterparty()
        {
        }

        public Counterparty(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
