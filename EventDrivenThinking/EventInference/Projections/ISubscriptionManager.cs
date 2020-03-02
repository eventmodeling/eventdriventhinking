using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Projections
{
    /// <summary>
    /// Common interface for all subscribers.
    /// </summary>
    public interface ISubscriptionManager
    {
        void Subscribe(IEnumerable<Type> eventTypes, bool fromBeginning, Action<IEnumerable<EventEnvelope>> onEventReceived);
    }

}