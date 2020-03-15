using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventStore.ClientAPI;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IEventDataFactory
    {
        EventData Create(EventMetadata em, IEvent ev, Func<Type, string> evName);
        EventData Create(EventMetadata em, IEvent ev);
        EventData CreateLink(EventMetadata em, IEvent ev, Type projectionType, Guid projectionVersion);
        EventData Create(EventEnvelope ev);
    }
}