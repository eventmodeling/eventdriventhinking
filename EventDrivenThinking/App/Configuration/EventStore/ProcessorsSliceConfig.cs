using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProcessorsSliceStartup : IProcessorSliceStartup
    {
        private IProcessorSchema[] _processors;
        private static ILogger Log = LoggerFactory.For<ProcessorsSliceStartup>();
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
            IProcessorSubscriptionController controller = serviceProvider.GetRequiredService<IProcessorSubscriptionController>();

            Log.Debug("Configuring processor slices:");
            foreach (var i in _processors)
            {
                Log.Debug($"Processor: {i.Type.Name} in {i.Category}");

                await controller.SubscribeHandlers(i, new ProcessorEventHandlerFactory(serviceProvider, i));
            }
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            this._processors = processors.ToArray();
        }
    }
}