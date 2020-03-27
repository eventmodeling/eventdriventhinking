using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private static ILogger logger = Logging.LoggerFactory.For<EventStore.ProjectionsSliceStartup>();

        private IProjectionSchema[] _projections;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _projections)
            {
                logger.Debug("Projection {projectionName} in {category} is using EventAggregator subscriptions.", i.Type.Name, i.Category);
                serviceCollection.TryAddSingleton(typeof(IModelProjectionSubscriber<>).MakeGenericType(i.ModelType),
                    typeof(EventAggregatorModelProjectionSubscriber<,>).MakeGenericType(i.ModelType, i.Type));


                serviceCollection.TryAddSingleton(typeof(IProjectionEventStream<>).MakeGenericType(i.Type)
                , typeof(InMemoryProjectionEventStream<>).MakeGenericType(i.Type));
            }
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            

            await ActivatorUtilities.CreateInstance<EventAggregatorSubscriber>(serviceProvider)
                .Subscribe(_projections.SelectMany(x=>x.Events));
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}