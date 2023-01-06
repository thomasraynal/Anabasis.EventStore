using Anabasis.Common;
using Anabasis.EventStore.Bus;
using Anabasis.ProtoActor.MessageHandlerActor;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public interface IAggregateMessageHandlerActorConfiguration: IMessageHandlerActorConfiguration
    {
        bool CrashAppIfSubscriptionFail { get; }
        StreamIdAndPosition[] StreamIdAndPositions { get; }
        bool KeepAppliedEventsOnAggregate { get; }
        UserCredentials? UserCredentials { get;  }
        CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; }
        bool UseSnapshot { get; set; }
    }
}
