using System;
using DynamicData;
using Anabasis.Tests.Demo;

namespace Anabasis.Tests.Tests
{
    public interface IConsumer
    {
        IObservableCache<Item, Guid> OnChange { get; }
    }
}
