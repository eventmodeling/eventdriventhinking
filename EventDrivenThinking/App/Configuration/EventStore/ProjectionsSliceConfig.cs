using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private IProjectionSchema[] _projections;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            

            foreach(var i in _projections)
            {
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
            foreach (var i in _projections)
            {
                var coordinator = ActivatorUtilities.CreateInstance<StreamJoinCoordinator>(serviceProvider).WithName(i.Type.Name);
                
                var subscriptions = i.Events.Select(x=> new SubscriptionInfo(x, 
                        typeof(ProjectionEventHandler<,>).MakeGenericType(i.Type, x),i.Type))
                    .ToArray();

                await coordinator.SubscribeToStreams(subscriptions);
            }
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}