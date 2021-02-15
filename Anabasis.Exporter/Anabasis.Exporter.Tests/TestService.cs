using EventStore.Core;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using System;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using ExpectedVersion = EventStore.Core.Data.ExpectedVersion;
using System.Diagnostics;
using EventStore.Common.Options;
using Anabasis.EventStore;
using Xunit;

namespace Anabasis.Exporter.Tests
{



  public class TestService
  {

    [Fact]
    public async Task ShouldCreatePersistantSubscription()
    {
      const string streamName = "newstream";
      const string eventType = "event-type";
      const string data = "{ \"a\":\"2\"}";
      const string metadata = "{}";


      var node = EmbeddedVNodeBuilder
      .AsSingleNode()
      .RunInMemory()
      .RunProjections(ProjectionType.All)
      .StartStandardProjections()
      .WithWorkerThreads(16)
      .Build();


      await node.StartAsync(true);

      var connectionSettings = PersistentSubscriptionSettings
          .Create()
          .StartFromBeginning();

      var userCredentials = new UserCredentials("admin", "changeit");

      var connectionOne = EmbeddedEventStoreConnection.Create(node);

      await connectionOne.CreatePersistentSubscriptionAsync(
       "myStream",
       "agroup",
       connectionSettings,
       userCredentials
      );

      var eventPayload = new EventData(
          eventId: Guid.NewGuid(),
          type: eventType,
          isJson: true,
          data: Encoding.UTF8.GetBytes(data),
          metadata: Encoding.UTF8.GetBytes(metadata)
      );

      var subscription = await connectionOne.ConnectToPersistentSubscriptionAsync(
        "myStream",
        "agroup",
        (_, evt)
            => Debug.WriteLine("event appeared"),
        (sub, reason, exception)
        => Debug.WriteLine($"Subscription dropped: {reason}"), userCredentials: userCredentials);


       var result = await connectionOne.AppendToStreamAsync("myStream", ExpectedVersion.NoStream, eventPayload);




      //var eventPayload = new EventData(
      //    eventId: Guid.NewGuid(),
      //    type: eventType,
      //    isJson: true,
      //    data: Encoding.UTF8.GetBytes(data),
      //    metadata: Encoding.UTF8.GetBytes(metadata)
      //);

      // result = await connection.AppendToStreamAsync(streamName, ExpectedVersion.NoStream, eventPayload);




      //var readEvents = await connection.ReadStreamEventsForwardAsync(streamName, 0, 10, true);

      //foreach (var evt in readEvents.Events)
      //{
      //  Console.WriteLine(Encoding.UTF8.GetString(evt.Event.Data));
      //}

      await Task.Delay(2000);

    }

    [Fact]
    public async Task ShouldCreateVolatileSubscription()
    {
      ClusterVNode node = EmbeddedVNodeBuilder
          .AsSingleNode()
          .RunInMemory()
          .OnDefaultEndpoints()
          .Build();

   
     await node.StartAsync(true);

      var connection = EmbeddedEventStoreConnection.Create(node);
      var userCredentials = new UserCredentials("admin", "changeit");
      await connection.ConnectAsync();

      var sampleEventData = new EventData(Guid.NewGuid(), "myTestEvent", false, Encoding.UTF8.GetBytes("bkabbjkhbjk"), null);


     await connection.SubscribeToStreamAsync("sampleStream",  true, (sub, evt) =>
      {
        Debug.WriteLine("Event appeared");
      },
       (sub, reason, exception) =>
       {
         Debug.WriteLine($"Ex : {reason}  {exception}");
       });

      await connection.SubscribeToAllAsync(true, (sub, evt) =>
        {
          Debug.WriteLine("Event appeared2");
        },
    (sub, reason, exception) =>
    {
      Debug.WriteLine($"Ex : {reason}  {exception}");
    }, userCredentials: userCredentials);


      WriteResult writeResult = await connection.AppendToStreamAsync("sampleStream", ExpectedVersion.Any, sampleEventData);
      var readEvents = await connection.ReadStreamEventsForwardAsync("sampleStream", 0, 10, true);

      foreach (var evt in readEvents.Events)
      {
        Debug.WriteLine(Encoding.UTF8.GetString(evt.Event.Data));
      }

    }


    [Fact]
    public async Task ShouldCreateCatchupSubscription()
    {
      ClusterVNode node = EmbeddedVNodeBuilder
          .AsSingleNode()
          .RunInMemory()
          .OnDefaultEndpoints()
          .Build();


      await node.StartAsync(true);

      var connection = EmbeddedEventStoreConnection.Create(node);
      var userCredentials = new UserCredentials("admin", "changeit");
      await connection.ConnectAsync();

      var sampleEventData = new EventData(Guid.NewGuid(), "myTestEvent", false, Encoding.UTF8.GetBytes("bkabbjkhbjk"), null);


      connection.SubscribeToAllFrom(null, CatchUpSubscriptionSettings.Default,  (sub, evt) =>
      {
        Debug.WriteLine("Event appeared");
    
         
      },userCredentials: userCredentials);




      WriteResult writeResult = await connection.AppendToStreamAsync("sampleStream", ExpectedVersion.Any, sampleEventData);

      await Task.Delay(1000);

      //var readEvents = await connection.ReadStreamEventsForwardAsync("sampleStream", 0, 10, true);

      //foreach (var evt in readEvents.Events)
      //{
      //  Debug.WriteLine(Encoding.UTF8.GetString(evt.Event.Data));
      //}

    }
  }
}
