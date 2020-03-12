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
        public EventData Create(EventMetadata em, IEvent ev, Func<Type, string> evName)
        {
            var str = JsonConvert.SerializeObject(ev);
            var evData = Encoding.UTF8.GetBytes(str);

            var strMeta = JsonConvert.SerializeObject(em);
            var evMeta = Encoding.UTF8.GetBytes(strMeta);

            var evTypeName = evName(ev.GetType());
            return new EventData(ev.Id, evTypeName, true, evData, evMeta);
        }
        public EventData Create(EventMetadata em, IEvent ev)
        {
            return Create(em, ev, ev => ev.Name);
        }

        public EventData Create(EventEnvelope ev)
        {
            return Create(ev.Metadata, ev.Event);
        }
    }
}