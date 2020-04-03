using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
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
                // Maybe IModelProjection Should be rather from TProjection not TModel?
                serviceCollection.TryAddSingleton(typeof(IModelProjectionSubscriber<>).MakeGenericType(i.ModelType),
                    typeof(EventStoreModelProjectionSubscriber<>).MakeGenericType(i.ModelType));

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
                foreach (var i in _projections)
                {
                    // When subscribing a projection
                    // We need to know how to do it. (1)
                    // At the end projection need to also know
                    // where to store it's events (2)

                    // Client & Server subscribe to the stream?
                    string projectionStreamName = $"{ServiceConventions.GetCategoryFromNamespace(i.Type.Namespace)}Projection-{i.ProjectionHash}";

                    var stream = (IProjectionEventStream)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider,
                        typeof(IProjectionEventStream<>).MakeGenericType(i.Type));

                    var factory = ActivatorUtilities.CreateInstance<SubscriptionFactory>(serviceProvider);
                    var streamSubscriptions = i.Events.Select(x => new SubscriptionInfo(x,
                            typeof(ProjectionStreamEventHandler<,>).MakeGenericType(i.Type, x), i.Type))
                        .ToArray();
                    var lastPosition = await stream.LastPosition();
                    await factory.SubscribeToStreams(lastPosition, streamSubscriptions);

                    
                    var projectionSubscriptions = i.Events.Select(x => new SubscriptionInfo(x,
                            typeof(ProjectionEventHandler<,>).MakeGenericType(i.Type, x), i.Type))
                        .ToArray();
                    
                    await factory.SubscribeToStream(projectionStreamName, projectionSubscriptions);

                    //var coordinator = ActivatorUtilities.CreateInstance<StreamJoinCoordinator>(serviceProvider).WithName(i.Type.Name);

                    //var subscriptions = i.Events.Select(x=> new SubscriptionInfo(x, 
                    //        typeof(ProjectionEventHandler<,>).MakeGenericType(i.Type, x),i.Type))
                    //    .ToArray();

                    //await coordinator.SubscribeToStreams(subscriptions);
                }
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}