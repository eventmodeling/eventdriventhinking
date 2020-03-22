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
using Serilog;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public class ProjectionEventHandler<TProjection, TEvent> : IEventHandler<TEvent>
        where TProjection : IProjection
        where TEvent:IEvent
    {
        private readonly TProjection _projection;
        private readonly ILogger _logger;
        private readonly IProjectionEventStream<TProjection> _projectionStream;
        private readonly IEnumerable<IProjectionStreamPartitioner<TProjection>> _partitioners;

        public ProjectionEventHandler(TProjection projection, ILogger logger, 
            IProjectionEventStream<TProjection> projectionStream,
            IEnumerable<IProjectionStreamPartitioner<TProjection>> partitioners)
        {
            _projection = projection;
            _logger = logger;
            _projectionStream = projectionStream;
            _partitioners = partitioners;
        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            Debug.WriteLine($"Executing projection in projection-event-handler: {typeof(TProjection).Name}");
            await _projection.Execute(new (EventMetadata, IEvent)[]{(m,ev)});
            _logger.Information("{projectionName} received from {aggregateId} an {eventName} {eventId}", typeof(TProjection).Name, m.AggregateId, typeof(TEvent).Name, ev.Id);

            await _projectionStream.Append(m, ev);

            //await Task.Delay(200); // WTF - EventStore problem?

            foreach (var i in _partitioners)
            {
                var partitions = i.CalculatePartitions(_projection.Model, m, ev);
                foreach(var p in partitions)
                    await _projectionStream.AppendPartition(p, m, ev);
            }
        }
    }
    
}