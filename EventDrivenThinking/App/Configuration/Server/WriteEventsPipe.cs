using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Server
{
    public class WriteEventsPipe {
        
        private readonly Predicate<ISchema> _schemaFilter;

        public IServiceProvider ServiceProvider { get; private set; }
        public IEnumerable<IAggregateSchema> Aggregates
        {
            get { return ((IEnumerable<IAggregateSchema>)ServiceProvider.GetService<IAggregateSchemaRegister>())
                            .Where<IAggregateSchema>(x => _schemaFilter(x)); }
        }
        internal WriteEventsPipe(IServiceProvider serviceProvider, 
            Predicate<ISchema> categoryFilter)
        {
            ServiceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }
    }

}