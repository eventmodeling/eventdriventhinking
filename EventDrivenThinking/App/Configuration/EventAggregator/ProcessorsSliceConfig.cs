using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
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
            await ActivatorUtilities.CreateInstance<EventAggregatorSubscriber>(serviceProvider)
                .Subscribe(_processors.SelectMany(x => x.Events));
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            _processors = processors.ToArray();
        }
    }
}