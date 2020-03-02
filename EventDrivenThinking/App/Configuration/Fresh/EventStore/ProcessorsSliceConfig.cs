using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Fresh.EventStore
{
    public class ProcessorsSliceStartup : IProcessorSliceStartup
    {
        private IProcessorSchema[] _processors;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _processors)
            {
                foreach (var v in i.Events)
                {
                    Type[] args = new Type[] { i.Type, v };
                    serviceCollection.AddSingleton(typeof(ICheckpointRepository<,>).MakeGenericType(args),
                        typeof(FileCheckpointRepository<,>).MakeGenericType(args));
                }
            }
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            //await ActivatorUtilities.CreateInstance<EventStoreSubscriber>(serviceProvider)
            //    .SubscribeAll(_processors.SelectMany(x => x.Events));


            foreach (var i in _processors)
            {
                var coordinator = ActivatorUtilities.CreateInstance<StreamJoinCoordinator>(serviceProvider).WithName(i.Type.Name);

                var subscriptions = i.Events.Select(x => new SubscriptionInfo(x,
                        typeof(ProcessorEventHandler<,>).MakeGenericType(i.Type, x), i.Type))
                    .ToArray();

                await coordinator.SubscribeToStreams(subscriptions);
            }
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            this._processors = processors.ToArray();
        }
    }
}