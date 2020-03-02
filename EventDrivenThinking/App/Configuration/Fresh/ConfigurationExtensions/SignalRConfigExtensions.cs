using System;
using System.Collections.Generic;
using System.Text;
using EventDrivenThinking.App.Configuration.Fresh.SignalR;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public static class SignalRConfigExtensions
    {
        public static FeaturePartition SubscribeFromSignalR(this ProjectionsConfig config, string url)
        {
            return config.Merge(new ProjectionsSliceStartup(url));
        }
        public static FeaturePartition SubscribeFromSignalR(this ProcessorsConfig config, string url)
        {
            return config.Merge(new ProcessorsSliceStartup(url));
        }
        
    }
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
