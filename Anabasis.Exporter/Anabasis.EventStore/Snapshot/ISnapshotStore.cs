using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot
{
  public interface ISnapshotStore<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    Task<TAggregate[]> Get(string[] eventFilters, int? version = null);
    Task<TAggregate> Get(string streamId, string[] eventFilters, int? version = null);
    Task Save(string[] eventFilters, TAggregate aggregate);
  }
}
