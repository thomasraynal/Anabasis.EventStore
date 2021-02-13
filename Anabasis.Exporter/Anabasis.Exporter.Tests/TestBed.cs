using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests.Tests
{
    public class TestBed
    {
        public TestBed(IProducer producer, IConsumer consumer)
        {
            Producer = producer;
            Consumer = consumer;
        }

        public IProducer Producer { get; }
        public IConsumer Consumer { get; }
    }
}
