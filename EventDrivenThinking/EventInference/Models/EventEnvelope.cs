using EventDrivenThinking.EventInference.Abstractions;

namespace EventDrivenThinking.EventInference.Models
{
    public class EventEnvelope
    {
        
        public EventEnvelope(IEvent @event, EventMetadata metadata)
        {
            Event = @event;
            Metadata = metadata;
        }

        public IEvent Event { get; protected set; }
        public EventMetadata Metadata { get; protected set; }
    }
}