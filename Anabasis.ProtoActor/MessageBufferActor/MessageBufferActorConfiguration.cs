using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class MessageBufferActorConfiguration
    {

        public MessageBufferActorConfiguration(TimeSpan reminderSchedulingDelay, IBufferingStrategy[]? bufferingStrategies = null)
        {
            BufferingStrategies = bufferingStrategies ?? new[] { new BufferSizeBufferingStrategy(1) };
            ReminderSchedulingDelay = reminderSchedulingDelay;
        }

        public IBufferingStrategy[] BufferingStrategies { get; set; }
        public TimeSpan ReminderSchedulingDelay { get; set; }
    }
}
