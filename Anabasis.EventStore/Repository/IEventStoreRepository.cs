using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
    public interface IEventStoreRepository
    {
        bool IsConnected { get; }
        string Id { get; }
        Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders);
        Task Emit(IEnumerable<IEvent> events, params KeyValuePair<string, string>[] extraHeaders);
        Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity;
    }
}
