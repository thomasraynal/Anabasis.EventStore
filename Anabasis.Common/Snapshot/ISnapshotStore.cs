using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface ISnapshotStore< TAggregate> where TAggregate : IAggregate, new()
  {
    Task<TAggregate[]> GetAll();
    Task<TAggregate[]> GetByVersionOrLast(string[] eventFilters, int? version = null);
    Task<TAggregate?> GetByVersionOrLast(string streamId, string[] eventFilters, int? version = null);
    Task Save(string?[] eventFilters, TAggregate aggregate);
  }
}
