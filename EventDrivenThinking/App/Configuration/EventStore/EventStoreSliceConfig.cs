using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class AggregateSliceStartup : IAggregateSliceStartup
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _aggregates)
            {
                var implementation = typeof(AggregateEventStream<>).MakeGenericType(i.Type);
                var service = typeof(IAggregateEventStream<>).MakeGenericType(i.Type);

                serviceCollection.AddScoped(service, implementation);
            }
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            // we don't do anything here.
            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IAggregateSchema> aggregates)
        {
            this._aggregates = aggregates.ToArray();
        }

        private IAggregateSchema[] _aggregates;
    }
}