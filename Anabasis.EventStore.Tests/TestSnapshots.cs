
using Anabasis.EventStore.Snapshot.InMemory;
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

            var snaphotRepository = new InMemorySnapshotStore<SomeDataAggregate>();

            var eventFilterOne = new[] { "a", "b", "c" };
            var entityA = $"{Guid.NewGuid()}";
            var someDataAggregate = new SomeDataAggregate(entityA);

            var snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 0);

            someDataAggregate.ApplyEvent(new SomeDataAggregateEvent(entityA, Guid.NewGuid())
            {
                EventNumber = 0
            }, saveAsPendingEvent: false);

            await snaphotRepository.Save(eventFilterOne, someDataAggregate);

            snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

            snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne, 0);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

            snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne, 1);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 0);

            someDataAggregate.ApplyEvent(new SomeDataAggregateEvent(entityA, Guid.NewGuid())
            {
                EventNumber = 1
            }, saveAsPendingEvent: false);

            Assert.True(someDataAggregate.Version == 1);

            await snaphotRepository.Save(eventFilterOne, someDataAggregate);

            snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne, 1);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);
            Assert.AreEqual(entityA, snapshots[0].EntityId);

        }

        [Test]
        public async Task ShouldCreateAndGetSnapshotsForSeveralStreams()
        {
            var snaphotRepository = new InMemorySnapshotStore<SomeDataAggregate>();

            var eventFilterOne = new[] { "d", "e", "f" };
            var eventFilterTwo = new[] { "g", "h", "i" };

            var entityA = $"{Guid.NewGuid()}";
            var entityB = $"{Guid.NewGuid()}";
            var entityC = $"{Guid.NewGuid()}";

            var someDataAggregateA = new SomeDataAggregate(entityA);
            var someDataAggregateB = new SomeDataAggregate(entityB);
            var someDataAggregateC = new SomeDataAggregate(entityC);

            someDataAggregateA.ApplyEvent(new SomeDataAggregateEvent(entityA, Guid.NewGuid()), saveAsPendingEvent: false);
            someDataAggregateB.ApplyEvent(new SomeDataAggregateEvent(entityB, Guid.NewGuid()), saveAsPendingEvent: false);
            someDataAggregateC.ApplyEvent(new SomeDataAggregateEvent(entityC, Guid.NewGuid()), saveAsPendingEvent: false);

            await snaphotRepository.Save(eventFilterOne, someDataAggregateA);
            await snaphotRepository.Save(eventFilterOne, someDataAggregateB);
            await snaphotRepository.Save(eventFilterTwo, someDataAggregateC);

            var snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterOne);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 2);

            snapshots = await snaphotRepository.GetByVersionOrLast(eventFilterTwo);

            Assert.NotNull(snapshots);
            Assert.True(snapshots.Length == 1);

            var snapshot = await snaphotRepository.GetByVersionOrLast($"{entityA}", eventFilterTwo);

            Assert.Null(snapshot);

            snapshot = await snaphotRepository.GetByVersionOrLast($"{entityA}", eventFilterOne);

            Assert.NotNull(snapshot);

        }
    }

}
