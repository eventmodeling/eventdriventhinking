using System;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Server
{
    public class SubscribeSyntax {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        private Action<SubscribePipe> _option;
        private SubscribeSyntax _next;
        public SubscribeSyntax(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }

        public SubscribeSyntax Subscribes(Action<SubscribePipe> opt)
        {
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            _option = opt;

            return _next = new SubscribeSyntax(_serviceProvider, _schemaFilter);
        }

        internal void Build()
        {
            if (_option == null)
                return;

            _option(new SubscribePipe(_serviceProvider, _schemaFilter));
            _next.Build();
        }
    }
}