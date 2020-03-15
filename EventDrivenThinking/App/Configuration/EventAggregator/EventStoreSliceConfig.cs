using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    public class AggregateSliceStartup : IAggregateSliceStartup
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _aggregates)
            {
                var implementation = typeof(InMemoryAggregateEventStream<>).MakeGenericType(i.Type);
                var service = typeof(IAggregateEventStream<>).MakeGenericType(i.Type);

                serviceCollection.AddScoped(service, implementation);
            }
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IAggregateSchema> aggregates)
        {
            this._aggregates = aggregates.ToArray();
        }

        private IAggregateSchema[] _aggregates;
    }
}