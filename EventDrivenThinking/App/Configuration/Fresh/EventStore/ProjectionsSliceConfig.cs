using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.App.Configuration.Fresh.EventStore
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private IProjectionSchema[] _projections;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach(var i in _projections)
            {
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