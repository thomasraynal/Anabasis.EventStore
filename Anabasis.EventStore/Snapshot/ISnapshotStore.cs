using Anabasis.EventStore.Shared;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot
{
  public interface ISnapshotStore<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    Task<TAggregate[]> GetAll();
    Task<TAggregate[]> GetByVersionOrLast(string[] eventFilters, int? version = null);
    Task<TAggregate> GetByVersionOrLast(string streamId, string[] eventFilters, int? version = null);
    Task Save(string[] eventFilters, TAggregate aggregate);
  }
}
