using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration
{
    public static class EventStoreConfigExtensions
    {
        public static FeaturePartition WriteToEventStore(this AggregateConfig config)
        {
            return config.Merge(new AggregateSliceStartup());
        }   
        public static FeaturePartition UseEventStore(this ProjectionsConfig config, 
            bool withGlobalHandlers = true)
        {
            return config.Merge(new ProjectionsSliceStartup(withGlobalHandlers));
        }   
        public static FeaturePartition SubscribeFromEventStore(this ProcessorsConfig config)
        {
            return config.Merge(new ProcessorsSliceStartup());
        }

        public static FeaturePartition FromEventStore(this QueryConfig config)
        {
            return config.Merge(new QuerySliceStartup());
        }
        public static FeaturePartition UseEventStore(this EventsConfig config)
        {
            return config.Merge(new ProjectionEventSliceStartup())
                .Events.Merge(new ProcessorEventSliceStartup());
        }

    }
    

    public static class BuildInConfigExtensions
    {
        public static FeaturePartition ToCommandHandler(this CommandsConfig config)
        {
            return config.Merge(new CommandHandlerInvocationSliceStartup());
        }
    }
}