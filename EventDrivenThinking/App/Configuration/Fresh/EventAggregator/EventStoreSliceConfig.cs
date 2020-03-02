using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.Fresh.EventAggregator
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