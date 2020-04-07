using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Logging;
using Serilog;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public class ProjectionEventHandler<TEvent> : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        private readonly IProjection _projection;
        private static readonly ILogger Log = LoggerFactory.For<ProjectionEventHandler<TEvent>>();

        public ProjectionEventHandler(IProjection projection)
        {
            _projection = projection;

        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            await _projection.Execute(new (EventMetadata, IEvent)[] { (m, ev) });
            Log.Information("{projectionName} received from {aggregateId} an {eventName} {eventId}", _projection.GetType().Name, m.AggregateId, typeof(TEvent).Name, ev.Id);

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