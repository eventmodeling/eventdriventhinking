using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui.Schema;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace EventDrivenThinking.App.Configuration
{
    public class SubscribePipe 
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;

        public IServiceProvider ServiceProvider => _serviceProvider;

        public IEnumerable<IAppProcessSchema> AppProcesses
        {
            get
            {
                return _serviceProvider
                    .GetService<IAppProcessSchemaRegister>()
                    .Where(x => _schemaFilter(x));
            }
        }
        public IEnumerable<IProcessorSchema> Processors
        {
            get
            {
                return _serviceProvider
                    .GetService<IProcessorSchemaRegister>()
                    .Where(x => _schemaFilter(x));
            }
        }
        public IEnumerable<IProjectionSchema> Projections
        {
            get 
            { 
                return _serviceProvider
                    .GetService<IProjectionSchemaRegister>()
                    .Where(x => _schemaFilter(x));
            }
        }

        public void Ignore() { }
        public SubscribePipe(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }
    }
}