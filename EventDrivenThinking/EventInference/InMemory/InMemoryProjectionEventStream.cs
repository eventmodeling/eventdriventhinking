using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using Prism.Events;

namespace EventDrivenThinking.EventInference.InMemory
{
    public class ProjectionEvent<TProjection>
    {
        public readonly EventEnvelope Event;
        public readonly Guid? PartitionId;
        public bool IsRoot => !PartitionId.HasValue;
        public ProjectionEvent(EventEnvelope @event, Guid? partitionId)
        {
            Event = @event;
            PartitionId = partitionId;
        }
    }
    public class InMemoryProjectionEventStream<TProjection> : IProjectionEventStream<TProjection> where TProjection : IProjection
    {
        class InMemoryPosition : IStreamPosition
        {
            public long Position;
        }
        private readonly ConcurrentQueue<EventEnvelope> _rootStream;
        private readonly ConcurrentDictionary<Guid,ConcurrentQueue<EventEnvelope>> _partitionStreams;
        private readonly PubSubEvent<ProjectionEvent<TProjection>> _pubSubEvent;

        public InMemoryProjectionEventStream(IEventAggregator eventAggregator)
        {
            _pubSubEvent = eventAggregator.GetEvent<PubSubEvent<ProjectionEvent<TProjection>>>();
            _rootStream = new ConcurrentQueue<EventEnvelope>();
            _partitionStreams = new ConcurrentDictionary<Guid, ConcurrentQueue<EventEnvelope>>();
        }

        public Type ProjectionType => typeof(TProjection);

        public async IAsyncEnumerable<EventEnvelope> Get(Guid key)
        {
            if (_partitionStreams.TryGetValue(key, out ConcurrentQueue<EventEnvelope> events))
            {
                foreach (var e in events)
                {
                    yield return e;
                }
            }
        }

        public async IAsyncEnumerable<EventEnvelope> Get()
        {
            foreach (var i in _rootStream)
                yield return i;
        }

        public async Task<IStreamPosition> LastPosition()
        {
            return new InMemoryPosition() {Position = _rootStream.Count};
        }

        public Task Append(EventMetadata m, IEvent e)
        {
            var eventEnvelope = new EventEnvelope(e,m);
            _rootStream.Enqueue(eventEnvelope);

            _pubSubEvent.Publish(new ProjectionEvent<TProjection>(eventEnvelope,null));

            return Task.CompletedTask;
        }

        public Task AppendPartition(Guid key, EventMetadata m, IEvent e)
        {
            var queue = _partitionStreams.GetOrAdd(key, (x) => new ConcurrentQueue<EventEnvelope>());
            var eventEnvelope = new EventEnvelope(e,m);
            queue.Enqueue(eventEnvelope);

            _pubSubEvent.Publish(new ProjectionEvent<TProjection>(eventEnvelope, key));

            return Task.CompletedTask;
        }
    }
}