using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProcessorEventSliceStartup : ISliceStartup<IEventSchema>
    {
        public void Initialize(IEnumerable<IEventSchema> processes)
        {

        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {

        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}