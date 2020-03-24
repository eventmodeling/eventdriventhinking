using System;
using System.Diagnostics;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventStore.Client;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IEventMetadataFactory<TAggregate>
    {
        EventMetadata Create(Guid key, Guid correlationId, IEvent ev, ulong version = 0);
    }
    public sealed class EventMetadataFactory<TAggregate> : IEventMetadataFactory<TAggregate>
    {
        public EventMetadata Create(Guid key, Guid correlationId, IEvent ev, ulong version )
        {
            var em = new EventMetadata(key, typeof(TAggregate), correlationId, version);
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
            return new EventData(Uuid.FromGuid(ev.Id), evTypeName, evData, evMeta);
        }
        public EventData Create(EventMetadata em, IEvent ev)
        {
            return Create(em, ev, ev => ev.Name);
        }

        public EventData CreateLink(EventMetadata em, IEvent ev, Type projectionType, Guid projectionVersion)
        {
            var category = ServiceConventions.GetCategoryFromNamespaceFunc(em.AggregateType.Namespace);
            var content = $"{em.Version}@{category}-{em.AggregateId}";

            var metadata = new LinkMetadata(projectionType, em.CorrelationId, projectionVersion);

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var metadataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metadata));

            Debug.WriteLine($"Creating a link {content}");

            return new EventData(Uuid.NewUuid(), "$>", contentBytes, metadataBytes);
            //return new EventData(ev.Id, "$>", false, contentBytes, metadataBytes);
        }

        public EventData Create(EventEnvelope ev)
        {
            return Create(ev.Metadata, ev.Event);
        }
    }
}