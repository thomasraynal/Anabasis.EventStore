using System;
using System.Threading.Tasks;
using Anabasis.EventStore.Tests.Demo;

namespace Anabasis.EventStore.Tests
{
    public interface IProducer
    {
        string Create();
        void Mutate(Item item, string payload);
    }
}