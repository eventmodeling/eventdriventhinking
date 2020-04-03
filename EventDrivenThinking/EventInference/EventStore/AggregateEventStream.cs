using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventStore;
using EventStore.Client;
using Newtonsoft.Json;
using EventData = EventStore.Client.EventData;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.EventInference.EventStore
{
    public class AggregateEventStream<TAggregate> : IAggregateEventStream<TAggregate>
        where TAggregate : IAggregate
    {
        private readonly IAggregateSchema<TAggregate> _aggregateSchema;
        private readonly ILogger _logger;
        private readonly IEventStoreFacade _connection;
        private readonly IEventConverter _eventConverter;

        private readonly IEventDataFactory _eventDataFactory;
        private readonly IEventMetadataFactory<TAggregate> _metadataFactory;

        public AggregateEventStream(IEventStoreFacade connection, 
            IEventConverter eventConverter,
            IEventDataFactory eventDataFactory,
            IEventMetadataFactory<TAggregate> metadataFactory,
            IAggregateSchema<TAggregate> aggregateSchema, Serilog.ILogger logger)
        {
            _connection = connection;
            _eventConverter = eventConverter;
            _eventDataFactory = eventDataFactory;
            _metadataFactory = metadataFactory;
            _aggregateSchema = aggregateSchema;
            _logger = logger;
        }

        public async IAsyncEnumerable<IEvent> Get(Guid key)
        {
            var streamName = GetStreamName(key);
            await foreach (var e in _connection.ReadStreamAsync(Direction.Forwards, streamName, StreamRevision.Start,
                100, resolveLinkTos: true))
            {
                var eventType = _aggregateSchema.EventByName(e.Event.EventType);
                var (m, eventInstance) = _eventConverter.Convert(eventType.EventType, e);   

                yield return eventInstance;
            }

        }


       
        public async Task<EventEnvelope[]> Append(Guid key, ulong version, Guid correlationId, IEnumerable<IEvent> published)
        {
            var streamName = GetStreamName(key);
            var publishedArray = published as IEvent[] ?? published.ToArray();
            EventEnvelope[] data = new EventEnvelope[publishedArray.Length];

            for (ulong i = 0; i < (ulong)publishedArray.Length; i++)
            {
                var ev = publishedArray[i];
                data[i] = new EventEnvelope(ev, _metadataFactory.Create(key, correlationId, ev, version + i));
            }
            var evData = data.Select(_eventDataFactory.Create);
            
            await _connection.AppendToStreamAsync(streamName, new StreamRevision(version), evData);

            
            _logger.Information("Writing event to stream {streamName} {eventNames}", streamName, publishedArray.Select(x => x.GetType().Name).ToArray());
            return data;
        }

        public async Task<EventEnvelope[]> Append(Guid key, Guid correlationId, IEnumerable<IEvent> published)
        {
            var streamName = GetStreamName(key);
            var publishedArray = published as IEvent[] ?? published.ToArray();
            EventEnvelope[] data = new EventEnvelope[publishedArray.Length];

            for (int i = 0; i < publishedArray.Length; i++)
            {
                var ev = publishedArray[i];
                data[i] = new EventEnvelope(ev, _metadataFactory.Create(key, correlationId, ev, (ulong)i));
            }
            var evData = data.Select(_eventDataFactory.Create);


            await _connection.AppendToStreamAsync(streamName, AnyStreamRevision.NoStream, evData);
            return data;
        }

        public string GetStreamName<TKey>(TKey key)
        {
            var category = ServiceConventions.GetCategoryFromNamespace(typeof(TAggregate).Namespace);
            return $"{category}-{key.ToString()}";
        }
    }
}