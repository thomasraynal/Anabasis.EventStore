using System;
using Anabasis.EventStore.Tests.Demo;
using DynamicData;

namespace Anabasis.EventStore.Tests
{
    public interface IConsumer
    {
        IObservableCache<Item, Guid> OnChange { get; }
    }
}
