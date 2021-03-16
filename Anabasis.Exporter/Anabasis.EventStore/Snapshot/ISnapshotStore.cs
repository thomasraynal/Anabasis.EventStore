using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot
{
  public interface ISnapshotStore<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    Task<TAggregate[]> Get(string[] eventFilter);
    Task<TAggregate> Get(string streamId, string eventFilter);
    Task Save(string[] eventFilter, TAggregate aggregate);
    Task Save(string streamId, string[] eventFilter, TAggregate aggregate);
  }
}
