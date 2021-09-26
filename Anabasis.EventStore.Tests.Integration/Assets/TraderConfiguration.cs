using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Integration.Tests
{
    public class TraderConfiguration
    {
        public string Name { get; set; }
        public TimeSpan PriceGenerationDelay { get; set; }
        public bool IsAutoGen { get; set; }

        public TraderConfiguration(string name): this()
        {
            Name = name;
        }

        public TraderConfiguration()
        {
            PriceGenerationDelay = TimeSpan.FromSeconds(1);
            IsAutoGen = true;
        }
    }
}
