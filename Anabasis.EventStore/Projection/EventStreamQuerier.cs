//using Anabasis.EventStore.Infrastructure;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Projections;
//using EventStore.ClientAPI.SystemData;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace Anabasis.EventStore.Projection
//{
//  public class EventStreamQuerier
//  {
//    private readonly UserCredentials _userCredentials;
//    private readonly ProjectionsManager _projectionsManager;

//    public EventStreamQuerier(UserCredentials userCredentials, ProjectionsManager projectionsManager)
//    {
//      _userCredentials = userCredentials;
//      _projectionsManager = projectionsManager;
//    }

//    public Task ExecuteOnQueryResultsAsync(string query, Action<IEvent> action)
//    {
//      return ExecuteAllEventsInternalAsync(
//          query,
//          ev =>
//          {
//            var obj = ev.Event.Data.JsonToJObject();
//            action(obj);
//          });
//    }

//    public Task ExecuteOnQueryResultsAsync<T>(string query, Action<T> action)
//    {
//      return ExecuteAllEventsInternalAsync(
//          query,
//          ev =>
//          {
//            var obj = ev.Event.Data.JsonTo<T>();
//            action((T)obj);
//          });
//    }

//    async Task ExecuteAllEventsInternalAsync(string query, Action<ResolvedEvent> action, TimeSpan? projectionTimeOut = null)
//    {
//      var queryName = Guid.NewGuid().ToString();
//      var resultStreamName = StreamNameHelper.GetResultStreamName(queryName);

//      await CreateQueryAsync(query, queryName).CAF();

//      ProjectionDetails details;
//      var timeout = DateTime.UtcNow.Add(projectionTimeOut ?? TimeSpan.FromSeconds(5));
//      bool isCompleted;
//      do
//      {
//        await Task.Delay(50).CAF();
//        details = await _projMan.GetProjectionDetails(queryName).CAF();
//        isCompleted = details.IsCompletedWithResults();
//        if (!isCompleted) await Task.Delay(200).CAF();
//      } while (!isCompleted && timeout > DateTime.UtcNow);

//      if (!isCompleted)
//      {
//        throw new EventStore.ClientAPI.Exceptions.OperationTimedOutException($"Query timed out, status:'{ details.Status }' - { details.StateReason }");
//      }

//      var exceptions = new ConcurrentBag<Exception>();
//      var finished = false;

//      await _connectionHolder.DoWithConnectionAsync(async (connection) =>
//      {
//        var subscription = connection.SubscribeToStreamFrom(
//            resultStreamName,
//            StreamCheckpoint.StreamStart,
//            new CatchUpSubscriptionSettings(maxLiveQueueSize: CatchUpSubscriptionSettings.Default.MaxLiveQueueSize, readBatchSize: CatchUpSubscriptionSettings.Default.ReadBatchSize, verboseLogging: CatchUpSubscriptionSettings.Default.VerboseLogging, resolveLinkTos: false),
//            (sub, ev) =>
//            {
//              if (ev.Event.EventType == GesHelper.EofEventType) { finished = true; return; }
//              if (ev.Event.Data.Length == 0) return;
//              try { action(ev); }
//              catch (Exception ex) { exceptions.Add(ex); }
//            });

//        try
//        {
//          while (!finished && (exceptions.Count == 0)) await Task.Delay(100).CAF();
//        }
//        finally
//        {
//          subscription.Stop();
//        }
//      }).CAF();


//      if (exceptions.Count != 0) throw new AggregateException(exceptions);
//    }

//    public Task CreateQueryAsync(string query, string queryName)
//    {
//      return _projMan.CreateTransientAsync(queryName, query, _connectionHolder.GesConnectionSettings.GetUserCredentials());
//    }
//  }
//}
