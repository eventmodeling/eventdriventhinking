using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

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
            // Processor is defined on certain site, however events can come from many sources.
            // Commands that are produced by processor can be then pushed to server or processed locally
            IProcessorSubscriptionController controller =
                serviceProvider.GetRequiredService<IProcessorSubscriptionController>();

            foreach (var i in _processors)
            {
                await controller.SubscribeHandlers(i, new ProcessorEventHandlerFactory(serviceProvider, i));
            }
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            _processors = processors.ToArray();
        }
    }
}