using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Bus
{
    public interface IEventHubPartitionMonitoring
    {
        Task SaveCheckPointMonitoring(EventProcessorPartition eventProcessorPartition, EventData lastEventProcessed, string hostname, string @namespace);
    }
}