using EventDrivenThinking.App.Configuration.EventStore;

namespace EventDrivenThinking.App.Configuration
{
    public static class EventStoreConfigExtensions
    {
        public static FeaturePartition WriteToEventStore(this AggregateConfig config)
        {
            return config.Merge(new AggregateSliceStartup());
        }   
        public static FeaturePartition SubscribeFromEventStore(this ProjectionsConfig config)
        {
            return config.Merge(new ProjectionsSliceStartup());
        }   
        public static FeaturePartition SubscribeFromEventStore(this ProcessorsConfig config)
        {
            return config.Merge(new ProcessorsSliceStartup());
        }   
    }

    public static class BuildInConfigExtensions
    {
        public static FeaturePartition ToCommandHandler(this CommandInvocationsConfig config)
        {
            return config.Merge(new CommandHandlerInvocationSliceStartup());
        }
    }
}