namespace Anabasis.Common
{
    public class DefaultSnapshotStrategy : ISnapshotStrategy
    {
        public static readonly ISnapshotStrategy Instance = new DefaultSnapshotStrategy();

        public DefaultSnapshotStrategy(int snapshotIntervalInEvents = 10)
        {
            SnapshotIntervalInEvents = snapshotIntervalInEvents;
        }

        public int SnapshotIntervalInEvents { get; }

        public bool IsSnapshotRequired(IAggregate aggregate)
        {
            return aggregate.Version - aggregate.VersionFromSnapshot >= SnapshotIntervalInEvents;
        }
    }
}
