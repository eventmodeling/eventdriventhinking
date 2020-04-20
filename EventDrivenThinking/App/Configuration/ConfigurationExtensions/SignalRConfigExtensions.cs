using System;


namespace EventDrivenThinking.App.Configuration
{
    
    public class EventSubscription
    {
        public Type EventType { get; private set; }
        public bool IsPersistent { get; private set; }

        public EventSubscription(Type eventType, bool isPersistent)
        {
            EventType = eventType;
            IsPersistent = isPersistent;
        }
    }
}
