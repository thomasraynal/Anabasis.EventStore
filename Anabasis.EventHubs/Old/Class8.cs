using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public interface IConsumable
    {
        /// <summary>
        /// In simple implementations, this will just delete the message.
        /// On non obvious ones, this could advance the checkpoint for the bus consumption
        /// </summary>
        void SetConsumed();
        /// <summary>
        /// In simple implementations, this will just delete the message.
        /// On non obvious ones, this could advance the checkpoint for the bus consumption
        /// </summary>
        Task SetConsumedAsync();

    }

    /// <summary>
    /// An consumption interface for non obvious queues implementations
    /// </summary>
    public interface IConsumable<out T> : IConsumable
    {
        /// <summary>
        /// Get the data hosted by this consumable
        /// </summary>
        T Content { get; }
    }
}
