using System;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IEventMetadataFactory<TAggregate>
    {
        EventMetadata Create(Guid key, Guid correlationId, IEvent ev);
    }
    public sealed class EventMetadataFactory<TAggregate> : IEventMetadataFactory<TAggregate>
    {
        public EventMetadata Create(Guid key, Guid correlationId, IEvent ev)
        {
            var em = new EventMetadata(key, typeof(TAggregate), correlationId);
            return em;
        }
    }
    public sealed class EventDataFactory : IEventDataFactory
    {
        public EventData Create(EventMetadata em, IEvent ev)
        {
            var str = JsonConvert.SerializeObject(ev);
            var evData = Encoding.UTF8.GetBytes(str);

            
            var strMeta = JsonConvert.SerializeObject(em);
            var evMeta = Encoding.UTF8.GetBytes(strMeta);

            return new EventData(ev.Id, ev.GetType().Name, true, evData, evMeta);
        }

        public EventData Create(EventEnvelope ev)
        {
            return Create(ev.Metadata, ev.Event);
        }
    }
}