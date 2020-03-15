using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Integrations.Carter;

namespace EventDrivenThinking.EventInference.SessionManagement
{
    public interface ISession
    {
        bool IsValid { get; }
        Guid Id { get; }
        ICollection<Subscription> Subscriptions { get; }
        void RegisterSubscriptionForEvent(Type eventType);
        Task SendEventCore(EventMetadata m, IEvent ev);
        Task SendEvents(Guid reqId, IEnumerable<EventEnvelope> events);
        
    }
}