using System;

namespace EventDrivenThinking.Integrations.EventStore
{
    public abstract class SingleEventStreamSubscriptionProvider
    {
        
        public abstract Type EventType { get; }
    }
}