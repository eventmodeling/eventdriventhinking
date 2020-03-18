using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IProjectionEventStream<TProjection>
        where TProjection : IProjection
    {
        string GetPartitionStreamName(Guid key);
        IAsyncEnumerable<EventEnvelope> Get(Guid key);
        IAsyncEnumerable<EventEnvelope> Get();
        Task Append(EventMetadata m, IEvent e);
        Task AppendPartition(Guid key, EventMetadata m, IEvent e);
    }

    
    public class ProjectionEventStream<TProjection> : IProjectionEventStream<TProjection>
        where TProjection : IProjection
    {
        private readonly IEventStoreConnection _connection;
        private readonly IEventDataFactory _eventDataFactory;
        private readonly ILogger _logger;
        private readonly string _category;
        private static readonly Guid _projectionId = typeof(TProjection).ComputeSourceHash();
        private IProjectionSchema<TProjection> _projectionSchema;
        public ProjectionEventStream(IEventStoreConnection connection, IEventDataFactory eventDataFactory, ILogger logger, IProjectionSchema<TProjection> projectionSchema)
        {
            _connection = connection;
            _eventDataFactory = eventDataFactory;
            _logger = logger;
            _projectionSchema = projectionSchema;
            _category =
                $"{ServiceConventions.GetCategoryFromNamespace(typeof(TProjection).Namespace)}Projection";
        }

        public string GetPartitionStreamName(Guid key)
        {
           return $"{_category}Partition-{key}";
        }

        public async IAsyncEnumerable<EventEnvelope> Get(Guid key)
        {
            StreamEventsSlice slice = null;
            do
            {
                slice = await _connection.ReadStreamEventsForwardAsync(GetPartitionStreamName(key), StreamPosition.Start, 100, true);
                foreach (var e in slice.Events)
                {
                    var eventString = Encoding.UTF8.GetString(e.Event.Data);
                    var em = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));
                    var eventType = _projectionSchema.EventByName(e.Event.EventType);
                    var eventInstance = (IEvent)JsonConvert.DeserializeObject(eventString, eventType);
                    yield return new EventEnvelope(eventInstance, em);
                }

            } while (!slice.IsEndOfStream);
        }

        public async IAsyncEnumerable<EventEnvelope> Get()
        {
            StreamEventsSlice slice = null;
            do
            {
                slice = await _connection.ReadStreamEventsForwardAsync(_category, StreamPosition.Start, 100, true);
                foreach (var e in slice.Events)
                {
                    var eventString = Encoding.UTF8.GetString(e.Event.Data);
                    var em = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));
                    var eventType = _projectionSchema.EventByName(e.Event.EventType);
                    var eventInstance = (IEvent)JsonConvert.DeserializeObject(eventString, eventType);
                    yield return new EventEnvelope(eventInstance, em);
                }

            } while (!slice.IsEndOfStream);

        }

        public async Task Append(EventMetadata m, IEvent e)
        {
            string aggregateType = m.AggregateType.Name;
            
            var data = _eventDataFactory.CreateLink(m,e,typeof(TProjection), _projectionId);
            
            await _connection.AppendToStreamAsync(_category, ExpectedVersion.Any, data);
        }
        public async Task AppendPartition(Guid key, EventMetadata m, IEvent e)
        {
            var streamName = GetPartitionStreamName(key);
            
            var data = _eventDataFactory.CreateLink(m, e, typeof(TProjection), _projectionId);
            await _connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, data);
        }
    }
}