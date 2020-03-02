using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Server
{
    public class SendCommandPipe
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        public IServiceProvider ServiceProvider => _serviceProvider;
       
        public IEnumerable<ICommandSchema> Commands
        {
            get { throw new NotImplementedException(); }
            //get { return _serviceProvider.GetService<ICommandInvocationSchemaRegister>()
        //    .Where(x => _schemaFilter(x)); }
    }

        internal SendCommandPipe(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }

        
       
    }
}