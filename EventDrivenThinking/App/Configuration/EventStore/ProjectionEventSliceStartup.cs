using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventStore
{
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
                Log.Debug("{eventName} is subscribed for projection subscriptions in EventStore.", i.Type.Name);
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