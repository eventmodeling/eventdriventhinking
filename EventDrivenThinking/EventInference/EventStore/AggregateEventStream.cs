using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.EventInference.EventStore
{
    public class AggregateEventStream<TAggregate> : IAggregateEventStream<TAggregate>
        where TAggregate : IAggregate
    {
        private readonly IAggregateSchema<TAggregate> _aggregateSchema;
        private readonly ILogger _logger;
        private readonly IEventStoreConnection _connection;

        private readonly IEventDataFactory _eventDataFactory;
        private readonly IEventMetadataFactory<TAggregate> _metadataFactory;

        public AggregateEventStream(IEventStoreConnection connection, 
            IEventDataFactory eventDataFactory,
            IEventMetadataFactory<TAggregate> metadataFactory,
            IAggregateSchema<TAggregate> aggregateSchema, Serilog.ILogger logger)
        {
            _connection = connection;
            _eventDataFactory = eventDataFactory;
            _metadataFactory = metadataFactory;
            _aggregateSchema = aggregateSchema;
            _logger = logger;
        }

        public async IAsyncEnumerable<IEvent> Get(Guid key)
        {
            var streamName = GetStreamName(key);
            var isFinished = true;
            do
            {
                var slice = await _connection.ReadStreamEventsForwardAsync(streamName, 0, 100, true);
                isFinished = slice.IsEndOfStream;

                foreach (var e in slice.Events)
                {
                    var eventString = Encoding.UTF8.GetString(e.Event.Data);
                    var eventType = _aggregateSchema.EventByName(e.Event.EventType);
                    var eventInstance = (IEvent) JsonConvert.DeserializeObject(eventString, eventType.EventType);

                    yield return eventInstance;
                }
            } while (!isFinished);
        }

        

        public async Task<EventEnvelope[]> Append(Guid key, long version, Guid correlationId, IEnumerable<IEvent> published)
        {
            var streamName = GetStreamName(key);
            var data = published.Select(x =>new EventEnvelope(x, _metadataFactory.Create(key, correlationId,x)))
                .ToArray();
            using (var tran = await _connection.StartTransactionAsync(streamName, version))
            {
                var evData = data.Select(_eventDataFactory.Create);
                await tran.WriteAsync(evData);
                await tran.CommitAsync();
            }
            _logger.Information("Writing event to stream {streamName} {eventNames}", streamName, published.Select(x => x.GetType().Name).ToArray());
            return data;
        }

        public string GetStreamName<TKey>(TKey key)
        {
            var category = ServiceConventions.GetCategoryFromNamespace(typeof(TAggregate).Namespace);
            return $"{category}-{key.ToString()}";
        }
    }
}