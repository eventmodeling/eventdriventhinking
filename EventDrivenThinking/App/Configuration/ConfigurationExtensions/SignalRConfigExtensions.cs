using System;
using EventDrivenThinking.App.Configuration.SignalR;

namespace EventDrivenThinking.App.Configuration
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
