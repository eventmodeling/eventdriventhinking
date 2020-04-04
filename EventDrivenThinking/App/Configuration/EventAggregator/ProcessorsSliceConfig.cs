using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventAggregator;
using EventDrivenThinking.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    public class ProcessorsSliceStartup : IProcessorSliceStartup
    {
        private IProcessorSchema[] _processors;
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddSingleton<IModelProjectionSubscriber<>, EventAggregatorModelProjectionSubscriber>();
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            await ActivatorUtilities.CreateInstance<EventAggregatorSubscriber>(serviceProvider)
                .Subscribe(_processors.SelectMany(x => x.Events));
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            _processors = processors.ToArray();
        }
    }

    public class ProcessorEventSliceStartup : ISliceStartup<IEventSchema>
    {
        public void Initialize(IEnumerable<IEventSchema> processes)
        {
            
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
    public class ProjectionEventSliceStartup : ISliceStartup<IEventSchema>
    {
        private IEventSchema[] _events;
        private static ILogger Log = LoggerFactory.For<ProjectionEventSliceStartup>();
        public void Initialize(IEnumerable<IEventSchema> events)
        {
            this._events = events.ToArray();
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {

            foreach (var i in _events)
            {
                Log.Debug("{eventName} is subscribed for projection subscriptions in EventAggregator.", i.Type.Name);
                var service = typeof(IEventSubscriptionProvider<,,>).MakeGenericType(typeof(IProjection), typeof(IProjectionSchema), i.Type);
                var impl = typeof(ProjectionEventSubscriptionProvider<>).MakeGenericType(i.Type);
                serviceCollection.AddSingleton(service, impl);
            }
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            // 
        }
    }
}