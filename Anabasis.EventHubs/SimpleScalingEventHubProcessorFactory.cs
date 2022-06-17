using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Configuration;
using BeezUP2.Framework.EventSourcing;
using BeezUP2.Framework.Insights;
using BeezUP2.Framework.Serialization;
using BeezUP2.Framework.TransientFaultHandling;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Polly;

namespace BeezUP2.Framework.EventHubs
{
    public class SimpleScalingEventHubProcessorFactory : IEventProcessorFactory
    {
        readonly EventHubProcessorHostParameters _parameters;
        readonly string _monitoringTableName;
        readonly BeezUPAppContext _appContext;
        readonly ActionAsync<IEnumerable<EventData>> _handleEvents;

        public SimpleScalingEventHubProcessorFactory(
            EventHubProcessorHostParameters parameters,
            BeezUPAppContext appContext,
            IMessageSerializer serializer,
            ActionAsync<IEnumerable<object>> handleEvents,
            AsyncPolicy retryPolicy = null,
            string monitoringTableName = EventHubsConstants.Default_MonitoringTableName
            )
            : this(
                  parameters,
                  appContext,
                  eventDatas => HandleEventData(serializer, handleEvents, eventDatas, appContext, retryPolicy),
                  monitoringTableName
                  )
        {
        }

        private static async Task HandleEventData(IMessageSerializer serializer, ActionAsync<IEnumerable<object>> handleEvents, IEnumerable<EventData> eventDatas, BeezUPAppContext appContext, AsyncPolicy retryPolicy)
        {
            retryPolicy = retryPolicy ?? PolyDefaults.GetAsyncRetryTransients();

            var messages = eventDatas.Select(ed =>
            {
                var record = EventHubsHelper.ToBeezUPRecordedEvent(ed, "WTFstream", out _);

                var msg = record.GetContent(serializer);
                return msg;
            }).ToArray();

            if (messages.Length == 0)
                return;

            var parentSpans = eventDatas.Select(ed => appContext.Tracer.ExtractContext(ed)).ToArray();

            try
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var spanBuilder = appContext.Tracer
                       .BuildSpan($"Consuming some event datas")
                       ;

                    spanBuilder.SetParentSpans(parentSpans);

                    using (var scope = spanBuilder.StartActive())
                    {
                        try
                        {
                            await handleEvents(messages).CAF();
                        }
                        catch (Exception ex)
                        {
                            scope.Span.LogError(ex, appContext);
                            throw;
                        }
                    }
                }).CAF();
            }
            catch (Exception ex)
            {
                appContext.Logger.LogException(ex);
                throw;
            }
        }

        public SimpleScalingEventHubProcessorFactory(
            EventHubProcessorHostParameters parameters,
            BeezUPAppContext appContext,
            ActionAsync<IEnumerable<EventData>> handleEvents,
            string monitoringTableName = EventHubsConstants.Default_MonitoringTableName
            )
        {
            _parameters = parameters;
            _monitoringTableName = monitoringTableName;
            _appContext = appContext;
            _handleEvents = handleEvents;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var processor = new SimpleScalingEventHubProcessor(
                _parameters, _monitoringTableName,
                _appContext,
                _handleEvents
                );
            return processor;
        }
    }
}
