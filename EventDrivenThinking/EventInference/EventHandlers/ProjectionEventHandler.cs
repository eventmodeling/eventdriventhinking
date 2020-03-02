using System.Diagnostics;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using Serilog;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public class ProjectionEventHandler<TProjection, TEvent> : IEventHandler<TEvent>
        where TProjection : IProjection
        where TEvent:IEvent
    {
        private readonly TProjection _projection;
        private readonly ILogger _logger;
        public ProjectionEventHandler(TProjection projection, ILogger logger)
        {
            _projection = projection;
            _logger = logger;
        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            await _projection.Execute(new (EventMetadata, IEvent)[]{(m,ev)});
            _logger.Information("{projectionName} received from {aggregateId} an {eventName} {eventId}", typeof(TProjection).Name, m.AggregateId, typeof(TEvent).Name, ev.Id);
        }
    }
}