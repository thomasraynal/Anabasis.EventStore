using Anabasis.EventStore.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
  public interface IEventStoreRepository
  {
    bool IsConnected { get; }

    Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders);
    Task Emit(IEvent[] events, params KeyValuePair<string, string>[] extraHeaders);
  }
}
