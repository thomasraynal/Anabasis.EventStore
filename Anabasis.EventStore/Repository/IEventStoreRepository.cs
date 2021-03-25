using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
  public interface IEventStoreRepository
  {
    bool IsConnected { get; }

    Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders);
    Task Emit(IEvent[] events, params KeyValuePair<string, string>[] extraHeaders);
  }
}
