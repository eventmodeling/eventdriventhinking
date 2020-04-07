using System;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventStore.Client;
using Newtonsoft.Json;

namespace EventDrivenThinking.Integrations.EventStore
{
    public interface IEventConverter
    {
        (EventMetadata, IEvent) Convert(Type eventType, ResolvedEvent e);
        (EventMetadata, TEvent) Convert<TEvent>(ResolvedEvent e) where TEvent : IEvent;
    }
    public class EventConverter : IEventConverter
    {
        public (EventMetadata, IEvent) Convert(Type eventType, ResolvedEvent e)
        {
            var eventString = Encoding.UTF8.GetString(e.Event.Data);
            var em = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));
            em.Version = e.Event.EventNumber.ToUInt64();
            var eventInstance = (IEvent)JsonConvert.DeserializeObject(eventString, eventType);
            return (em, eventInstance);
        }

        public (EventMetadata, TEvent) Convert<TEvent>(ResolvedEvent e) where TEvent:IEvent
        {
            var eventString = Encoding.UTF8.GetString(e.Event.Data);
            var em = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));
            em.Version = e.Event.EventNumber.ToUInt64();
            var eventInstance = JsonConvert.DeserializeObject<TEvent>(eventString);
            return (em, eventInstance);
        }
    }
}