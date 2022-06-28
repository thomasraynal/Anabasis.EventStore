using Anabasis.EventHubs.Old;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Configuration;
using BeezUP2.Framework.Insights;
using BeezUP2.Framework.Queuing;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;

namespace BeezUP2.Framework.EventHubs
{
    public class ScalingEventHubProcessorFactory2 : IEventProcessorFactory
    {
        readonly Func<IObservable<IConsumable<EventData>>, IDisposable> _subscribeEventDatas;
        readonly EventHubProcessorHostParameters _parameters;
        readonly string _monitoringTableName;
        readonly int _maxInProgressEventdataCount;

        public ScalingEventHubProcessorFactory2(
            EventHubProcessorHostParameters parameters,
            Func<IObservable<IConsumable<EventData>>, IDisposable> subscribeEventDatas,
            string monitoringTableName = EventHubsConstants.Default_MonitoringTableName,
            int maxInProgressEventdataCount = EventHubsConstants.Default_MaxInProgressEventdataCount
            )
        {
            _subscribeEventDatas = subscribeEventDatas;
            _parameters = parameters;
            _monitoringTableName = monitoringTableName;
            _maxInProgressEventdataCount = maxInProgressEventdataCount;
        }
        
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var processor = new ScalingEventHubProcessor2(
                _parameters, _monitoringTableName,
                _subscribeEventDatas,
                _maxInProgressEventdataCount
                );
            return processor;
        }
    }
}
