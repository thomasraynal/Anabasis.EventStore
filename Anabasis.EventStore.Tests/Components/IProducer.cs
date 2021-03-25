using System;
using System.Threading.Tasks;
using Anabasis.Tests.Demo;

namespace Anabasis.Tests
{
    public interface IProducer
    {
        Guid Create();
        Task<Item> Get(Guid item, bool loadEvents);
        void Mutate(Item item, string payload);
    }
}