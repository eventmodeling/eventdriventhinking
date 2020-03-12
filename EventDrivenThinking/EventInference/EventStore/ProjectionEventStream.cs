using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IProjectionEventStream<TProjection>
        where TProjection : IProjection
    {
        string GetPartitionStreamName(Guid key);
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
        
        public ProjectionEventStream(IEventStoreConnection connection, IEventDataFactory eventDataFactory, ILogger logger)
        {
            _connection = connection;
            _eventDataFactory = eventDataFactory;
            _logger = logger;
            _category =
                $"{ServiceConventions.GetCategoryFromNamespace(typeof(TProjection).Namespace)}Projection";
        }

        public string GetPartitionStreamName(Guid key)
        {
           return $"{_category}ProjectionPartition-{key}";
        }
        public async Task Append(EventMetadata m, IEvent e)
        {
            string aggregateType = m.AggregateType.Name;
            var data = _eventDataFactory.Create(m.AsLink(),e, ev => $"{ev.Name}@{aggregateType}");
            await _connection.AppendToStreamAsync(_category, ExpectedVersion.Any, data);
        }
        public async Task AppendPartition(Guid key, EventMetadata m, IEvent e)
        {
            var streamName = GetPartitionStreamName(key);
            string aggregateType = m.AggregateType.Name;

            var data = _eventDataFactory.Create(m.AsLink(), e, ev => $"@{ev.Name}@{aggregateType}");
            await _connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, data);
        }
    }
}