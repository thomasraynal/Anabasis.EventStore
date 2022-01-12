using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class Currency
    {
        private Currency()
        {
        }

        public Currency(string name, string code)
        {
            Name = name;
            Code = code;
        }

        public string Name { get;  internal set; }
        public string Code { get; internal set; }
    }
}
