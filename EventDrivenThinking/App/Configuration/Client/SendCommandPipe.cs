using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Client
{
    public class SendCommandPipe
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        public IServiceProvider ServiceProvider => _serviceProvider;
       
        public IEnumerable<Type> Commands
        {
            get { return _serviceProvider.GetService<ICommandInvocationSchemaRegister>()
                .Where(x => _schemaFilter(x))
                .Select(x=>x.Type); }
        }

        internal SendCommandPipe(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }
    }
}