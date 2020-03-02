using System;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Client
{
    public class SendCommandSyntax
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        private SubscribeSyntax _subscribeSyntax;
        private Action<SendCommandPipe> _option;
        internal SendCommandSyntax(IServiceProvider serviceProvider, 
            Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }

        public SubscribeSyntax SendCommands(Action<SendCommandPipe> opt)
        {
            if (_subscribeSyntax != null) throw new InvalidOperationException("ReceiveCommand has already been invoked.");
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            _option = opt;
            return (_subscribeSyntax = new SubscribeSyntax(_serviceProvider, _schemaFilter));
        }

        internal void Build()
        {
            if (_option == null || _subscribeSyntax == null)
                throw new InvalidOperationException("ReceiveCommands was not configured.");

            _option(new SendCommandPipe(_serviceProvider, _schemaFilter));
            _subscribeSyntax.Build();
        }
    }
}