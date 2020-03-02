using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventStore.ClientAPI;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IEventDataFactory
    {
        EventData Create(EventMetadata em, IEvent ev);
        EventData Create(EventEnvelope ev);
    }
}