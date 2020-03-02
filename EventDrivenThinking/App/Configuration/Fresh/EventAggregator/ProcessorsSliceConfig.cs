using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.Fresh.EventAggregator
{
    public class ProcessorsSliceStartup : IProcessorSliceStartup
    {
        private IProcessorSchema[] _processors;
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISubscriptionManager, EventAggregatorSubscriptionManager>();
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
}