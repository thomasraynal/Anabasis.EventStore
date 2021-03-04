using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Actor.Actor
{
  public class TestActor : BaseActor
  {
    public TestActor(PersistentSubscriptionEventStoreQueue persistentSubscriptionEventStoreQueue)
    {

      persistentSubscriptionEventStoreQueue.OnEvent().Subscribe(async @event =>
      {
        await OnEventReceived(@event);

      });

    }
  }
}
