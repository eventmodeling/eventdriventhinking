using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Logging;
using Serilog;

namespace EventDrivenThinking.EventInference.EventHandlers
{

    public class ProjectionStreamEventHandler<TProjection, TEvent> : IEventHandler<TEvent>
        where TProjection : IProjection
        where TEvent : IEvent
    {
        private readonly TProjection _projection;
        private static readonly ILogger Log = LoggerFactory.For<ProjectionStreamEventHandler<TProjection, TEvent>>();
        private readonly IProjectionEventStream<TProjection> _projectionStream;
        private readonly IEnumerable<IProjectionStreamPartitioner<TProjection>> _partitioners;

        public ProjectionStreamEventHandler(TProjection projection,
            IProjectionEventStream<TProjection> projectionStream,
            IEnumerable<IProjectionStreamPartitioner<TProjection>> partitioners)
        {
            _projection = projection;

            _projectionStream = projectionStream;
            _partitioners = partitioners;

        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            Log.Information("{projectionName} builds stream from {aggregateId} an {eventName} {eventId}", typeof(TProjection).Name, m.AggregateId, typeof(TEvent).Name, ev.Id);

            await _projectionStream.Append(m, ev);

            foreach (var i in _partitioners)
            {
                var partitions = i.CalculatePartitions(_projection.Model, m, ev);
                foreach (var p in partitions)
                    await _projectionStream.AppendPartition(p, m, ev);
            }
        }
    }
    public class ProjectionEventHandler<TProjection, TEvent> : IEventHandler<TEvent>
        where TProjection : IProjection
        where TEvent:IEvent
    {
        private readonly TProjection _projection;
        private static readonly ILogger Log = LoggerFactory.For<ProjectionEventHandler<TProjection, TEvent>>();
        
        public ProjectionEventHandler(TProjection projection)
        {
            _projection = projection;
            
        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            await _projection.Execute(new (EventMetadata, IEvent)[]{(m,ev)});
            Log.Information("{projectionName} received from {aggregateId} an {eventName} {eventId}", typeof(TProjection).Name, m.AggregateId, typeof(TEvent).Name, ev.Id);

        }
    }
    
}