using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private static ILogger Log = EventDrivenThinking.Logging.LoggerFactory.For<ProjectionsSliceStartup>();

        private IProjectionSchema[] _projections;
        private bool _withGlobalHandlers;

        public ProjectionsSliceStartup(in bool withGlobalHandlers)
        {
            this._withGlobalHandlers = withGlobalHandlers;
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            Log.Debug("Configuring projection slices:");
            foreach (var i in _projections)
            {
                Log.Debug("Projection {projectionName} in {category} is using EventStore subscriptions.",  i.Type.Name, i.Category);
                
                foreach(var v in i.Events)
                {
                    Type[] args = new Type[]{i.Type, v};
                    serviceCollection.AddSingleton(typeof(ICheckpointRepository<,>).MakeGenericType(args), 
                        typeof(FileCheckpointRepository<,>).MakeGenericType(args));

                    serviceCollection.AddSingleton(typeof(IProjectionEventStream<>).MakeGenericType(i.Type),
                        typeof(ProjectionEventStream<>).MakeGenericType(i.Type));
                }
            }
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            if (_withGlobalHandlers)
            {
                IProjectionSubscriptionController controller =
                    serviceProvider.GetRequiredService<IProjectionSubscriptionController>();

                IProjectionStreamSubscriptionController streamController =
                    serviceProvider.GetRequiredService<IProjectionStreamSubscriptionController>();

                foreach (var i in _projections)
                {
                    await controller.SubscribeHandlers(i, new ProjectionEventHandlerFactory(serviceProvider, i));
                    await streamController.SubscribeHandlers(i, new ProjectionStreamEventHandlerFactory(serviceProvider,i)); // this will load checkpoints.
                    
                }}
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}