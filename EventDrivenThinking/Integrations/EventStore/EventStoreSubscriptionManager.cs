using System;
using System.Collections.Generic;
using System.Text;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventStore.ClientAPI;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class EventStoreSubscriptionManager : ISubscriptionManager
    {
        private IEventStoreConnection _connection;

        public EventStoreSubscriptionManager(IEventStoreConnection connection)
        {
            _connection = connection;
        }

        public void Subscribe(IEnumerable<Type> eventTypes, bool fromBeginning, Action<IEnumerable<EventEnvelope>> onEventReceived)
        {
            throw new NotSupportedException();
        }
    }
}
