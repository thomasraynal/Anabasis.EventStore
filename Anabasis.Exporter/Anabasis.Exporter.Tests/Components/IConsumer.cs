using System;
using DynamicData;
using Anabasis.Tests.Demo;

namespace Anabasis.Tests
{
    public interface IConsumer
    {
        IObservableCache<Item, Guid> OnChange { get; }
    }
}
