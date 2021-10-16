using Anabasis.EventStore.Snapshot;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestSnapshots
    {

        [Test]
        public async Task ShouldCreateAndGetSnapshots()
        {

            var snaphotRepository = new InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>>();

            var eventFilterOne = new[] { "a", "b", "c" };
            var entityA = Guid.NewGuid();
            var someDataAggregate = new SomeDataAggregate<Guid>(entityA);

            var snapshots = await snaphotRepository.Get(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 0);

            someDataAggregate.ApplyEvent(new SomeData<Guid>(entityA, Guid.NewGuid()), saveAsPendingEvent: false);

            await snaphotRepository.Save(eventFilterOne, someDataAggregate);

            snapshots = await snaphotRepository.Get(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

            snapshots = await snaphotRepository.Get(eventFilterOne, 0);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

            snapshots = await snaphotRepository.Get(eventFilterOne, 1);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 0);

            someDataAggregate.ApplyEvent(new SomeData<Guid>(entityA, Guid.NewGuid()), saveAsPendingEvent: false);

            Assert.True(someDataAggregate.Version == 1);

            await snaphotRepository.Save(eventFilterOne, someDataAggregate);

            snapshots = await snaphotRepository.Get(eventFilterOne, 1);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

        }

        [Test]
        public async Task ShouldCreateAndGetSnapshotsFOrSeveralStreams()
        {
            var snaphotRepository = new InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>>();

            var eventFilterOne = new[] { "d", "e", "f" };
            var eventFilterTwo = new[] { "g", "h", "i" };

            var entityA = Guid.NewGuid();
            var entityB = Guid.NewGuid();
            var entityC = Guid.NewGuid();

            var someDataAggregateA = new SomeDataAggregate<Guid>(entityA);
            var someDataAggregateB = new SomeDataAggregate<Guid>(entityA);
            var someDataAggregateC = new SomeDataAggregate<Guid>(entityA);

            someDataAggregateA.ApplyEvent(new SomeData<Guid>(entityA, Guid.NewGuid()), saveAsPendingEvent: false);
            someDataAggregateB.ApplyEvent(new SomeData<Guid>(entityB, Guid.NewGuid()), saveAsPendingEvent: false);
            someDataAggregateC.ApplyEvent(new SomeData<Guid>(entityC, Guid.NewGuid()), saveAsPendingEvent: false);

            await snaphotRepository.Save(eventFilterOne, someDataAggregateA);
            await snaphotRepository.Save(eventFilterOne, someDataAggregateB);
            await snaphotRepository.Save(eventFilterTwo, someDataAggregateC);

            var snapshots = await snaphotRepository.Get(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 2);

            snapshots = await snaphotRepository.Get(eventFilterTwo);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);

            var snapshot = await snaphotRepository.Get($"{entityA}", eventFilterTwo);

            Assert.Null(snapshot);

            snapshot = await snaphotRepository.Get($"{entityA}", eventFilterOne);

            Assert.NotNull(snapshot);

        }
    }

}
