using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Core
{
    public class EventEnvelope<TEvent> : EventEnvelope
        where TEvent : IEvent
    {
       
        public EventEnvelope(TEvent @event, EventMetadata metadata) : base(@event, metadata)
        {
        }

        public new TEvent Event => (TEvent) base.Event;
    }
}