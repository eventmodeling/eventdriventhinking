using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class QuerySliceStartup : IQuerySliceStartup
    {
        private IQuerySchema[] queries;
        public void Initialize(IEnumerable<IQuerySchema> queries)
        {
            this.queries = queries.ToArray();
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