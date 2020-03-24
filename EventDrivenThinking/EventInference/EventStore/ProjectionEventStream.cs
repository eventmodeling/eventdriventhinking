using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using EventStore.Client;
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
        private readonly IEventStoreFacade _connection;
        private readonly IEventDataFactory _eventDataFactory;
        private readonly ILogger _logger;
        private readonly string _projectionStreamName;
        
        //private static readonly Guid _projectionId = typeof(TProjection).ComputeSourceHash();
        private static readonly Guid _projectionId = typeof(TProjection).FullName.ToGuid();

        private IProjectionSchema<TProjection> _projectionSchema;
        public ProjectionEventStream(IEventStoreFacade connection, IEventDataFactory eventDataFactory, ILogger logger, IProjectionSchema<TProjection> projectionSchema)
        {
            _connection = connection;
            _eventDataFactory = eventDataFactory;
            _logger = logger;
            _projectionSchema = projectionSchema;
            _projectionStreamName =
                $"{ServiceConventions.GetCategoryFromNamespace(typeof(TProjection).Namespace)}Projection";
        }

        public string GetPartitionStreamName(Guid key)
        {
           return $"{_projectionStreamName}Partition-{key}";
        }
        public string GetStreamName(Guid key)
        {
            return $"{_projectionStreamName}-{key}";
        }

        public async IAsyncEnumerable<EventEnvelope> Get(Guid key)
        {
            var streamName = GetPartitionStreamName(key);
            await foreach (var p in ReadStream(streamName)) yield return p;
        }

        private async IAsyncEnumerable<EventEnvelope> ReadStream(string streamName)
        {
            await foreach (var e in _connection.ReadStreamAsync(Direction.Forwards, streamName, StreamRevision.Start,
                100, resolveLinkTos: true))
            {
                var eventString = Encoding.UTF8.GetString(e.Event.Data);
                var eventType = _projectionSchema.EventByName(e.Event.EventType);
                var eventInstance = (IEvent) JsonConvert.DeserializeObject(eventString, eventType);
                var metadata = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));

                yield return new EventEnvelope(eventInstance, metadata);
            }
        }

        public async IAsyncEnumerable<EventEnvelope> Get()
        {
            var streamName = GetStreamName(_projectionId);
            await foreach (var p in ReadStream(streamName)) yield return p;
        }

        public async Task Append(EventMetadata m, IEvent e)
        {
            var streamName = GetStreamName(_projectionId);
            var data = _eventDataFactory.CreateLink(m,e,typeof(TProjection), _projectionId);

            Debug.WriteLine($"Appending to stream {streamName} {e.GetType().Name}");

            var result = await _connection.AppendToStreamAsync(streamName, AnyStreamRevision.Any, new []{ data });
            
            Debug.WriteLine($"LogPosition {result.LogPosition.CommitPosition}");
        }
        public async Task AppendPartition(Guid key, EventMetadata m, IEvent e)
        {
            var streamName = GetPartitionStreamName(key);
            
            var data = _eventDataFactory.CreateLink(m, e, typeof(TProjection), _projectionId);

            Debug.WriteLine($"Appending to stream {streamName} {e.GetType().Name}");
            
            var result = await _connection.AppendToStreamAsync(streamName, AnyStreamRevision.Any, new[] { data});
            Debug.WriteLine($"LogPosition {result.LogPosition.CommitPosition}");
        }
    }
}