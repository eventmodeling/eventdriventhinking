﻿using System;
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
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Logging;
using EventDrivenThinking.Utils;
using EventStore.Client;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IProjectionEventStream<TProjection> : IProjectionEventStream
        where TProjection : IProjection
    {
        
    }
    struct StreamPosition : IStreamPosition
    {
        public Position GlobalPosition;
        public StreamRevision StreamRevision;

        public bool IsStart => GlobalPosition.PreparePosition == 0 && GlobalPosition.CommitPosition == 0UL;
    }
    public interface IStreamPosition { }
    public interface IProjectionEventStream
    {
        Type ProjectionType { get; }
        IAsyncEnumerable<EventEnvelope> Get(Guid key);
        IAsyncEnumerable<EventEnvelope> Get();
        Task<IStreamPosition> LastPosition();
        Task Append(EventMetadata m, IEvent e);
        Task AppendPartition(Guid key, EventMetadata m, IEvent e);

    }


    public class ProjectionEventStream<TProjection> : IProjectionEventStream<TProjection>
        where TProjection : IProjection
    {
        public Type ProjectionType => typeof(TProjection);
        private readonly IEventStoreFacade _connection;
        private readonly IEventConverter _eventConveter;
        private readonly IEventDataFactory _eventDataFactory;
        private static readonly ILogger Log = LoggerFactory.For<ProjectionEventStream<TProjection>>();
        private readonly string _projectionStreamName;
        
        

        private readonly IProjectionSchema<TProjection> _projectionSchema;
        public ProjectionEventStream(IEventStoreFacade connection, 
            IEventConverter eventConveter,
            IEventDataFactory eventDataFactory,
            IProjectionSchema<TProjection> projectionSchema)
        {
            _connection = connection;
            _eventConveter = eventConveter;
            _eventDataFactory = eventDataFactory;
            _projectionSchema = projectionSchema;
            _projectionStreamName =
                $"{ServiceConventions.GetCategoryFromNamespace(typeof(TProjection).Namespace)}Projection";
        }

        private string GetPartitionStreamName(Guid key)
        {
           return $"{_projectionStreamName}Partition-{key}";
        }

        private string GetStreamName(Guid key)
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
                var eventType = _projectionSchema.EventByName(e.Event.EventType);
                var (metadata, eventInstance) = _eventConveter.Convert(eventType, e);
                yield return new EventEnvelope(eventInstance, metadata);
            }
        }

        public async IAsyncEnumerable<EventEnvelope> Get()
        {
            var streamName = GetStreamName(_projectionSchema.ProjectionHash);
            await foreach (var p in ReadStream(streamName)) yield return p;
        }

        public async Task<IStreamPosition> LastPosition()
        {
            try
            {
                var streamName = GetStreamName(_projectionSchema.ProjectionHash);
                var esPosition = await _connection.GetLastStreamPosition(streamName);
                
                return new StreamPosition()
                {
                    GlobalPosition = esPosition.Item1,
                    StreamRevision = esPosition.Item2
                };
            }
            catch (StreamNotFoundException ex)
            {
                return new StreamPosition()
                {
                    GlobalPosition = Position.Start,
                    StreamRevision = StreamRevision.Start
                };
            }
        }

        public async Task Append(EventMetadata m, IEvent e)
        {
            var streamName = GetStreamName(_projectionSchema.ProjectionHash);
            var data = _eventDataFactory.CreateLink(m,e,typeof(TProjection), _projectionSchema.ProjectionHash);

            Log.Debug("Appending to stream {streamName} {eventType}", streamName, e.GetType().Name);

            var result = await _connection.AppendToStreamAsync(streamName, AnyStreamRevision.Any, new []{ data });
        }
        public async Task AppendPartition(Guid key, EventMetadata m, IEvent e)
        {
            var streamName = GetPartitionStreamName(key);
            
            var data = _eventDataFactory.CreateLink(m, e, typeof(TProjection), _projectionSchema.ProjectionHash);

            Log.Debug("Appending to stream {streamName} {eventType}", streamName, e.GetType().Name);
            
            var result = await _connection.AppendToStreamAsync(streamName, AnyStreamRevision.Any, new[] { data});
        }
    }
}