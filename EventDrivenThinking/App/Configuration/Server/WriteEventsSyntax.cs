using System;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Server
{
    public class WriteEventsSyntax {
        
        private readonly IServiceProvider ServiceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        private SubscribeSyntax _subsribeSyntax;
        private Action<WriteEventsPipe> _option;

        internal WriteEventsSyntax(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            ServiceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }

        public SubscribeSyntax WritesEvents(Action<WriteEventsPipe> opt)
        {
            if (_subsribeSyntax != null) throw new InvalidOperationException("WritesEvents has already been invoked.");
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            _option = opt;
            return (_subsribeSyntax = new SubscribeSyntax(ServiceProvider, _schemaFilter));
        }

        internal void Build()
        {
            if (_option == null || _subsribeSyntax == null)
                throw new InvalidOperationException("WritesEvents was not configured.");

            _option(new WriteEventsPipe(ServiceProvider, _schemaFilter));
            _subsribeSyntax.Build();
        }
    }
}