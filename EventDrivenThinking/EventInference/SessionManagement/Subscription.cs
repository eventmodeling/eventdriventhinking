using System;

namespace EventDrivenThinking.EventInference.SessionManagement
{
    public class Subscription
    {
        public Subscription(string topic, params Type[] events)
        {
            Events = events;
            Topic = topic;
        }

        public string Topic { get; private set; } // ReadModel name
        public Type[] Events { get; private set; }
    }
}