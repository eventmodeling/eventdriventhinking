using EventDrivenThinking.App.Configuration.EventAggregator;

namespace EventDrivenThinking.App.Configuration
{
    public static class EventAggregatorConfigExtensions
    {
        public static FeaturePartition WriteToEventAggregator(this AggregateConfig config)
        {
            return config.Merge(new AggregateSliceStartup());
        }
        public static FeaturePartition SubscribeFromEventAggregator(this ProjectionsConfig config)
        {
            return config.Merge(new ProjectionsSliceStartup());
        }
        public static FeaturePartition SubscribeFromEventAggregator(this ProcessorsConfig config)
        {
            return config.Merge(new ProcessorsSliceStartup());
        }
        public static FeaturePartition UseEventAggregator(this EventsConfig config)
        {
            return config.Merge(new ProjectionEventSliceStartup())
                .Events.Merge(new ProcessorEventSliceStartup());
        }
        public static FeaturePartition ToRest(this CommandsConfig config)
        {
            return config.Merge(new RestCommandsSliceStartup());
        }

        public static CommandsConfig BindEventAggregator(this CommandsConfig config)
        {
            config.Merge(new EventAggregator.CommandHandlerInvocationSliceStartup());
            return config;
        }
    }
}