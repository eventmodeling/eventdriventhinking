using System;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Server
{
    public class SendPipeDescription  
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Predicate<ISchema> _schemaFilter;
        private WriteEventsSyntax _writeSyntax;
        private Action<SendCommandPipe> _option;
        internal SendPipeDescription(IServiceProvider serviceProvider, Predicate<ISchema> categoryFilter)
        {
            _serviceProvider = serviceProvider;
            _schemaFilter = categoryFilter;
        }

        public WriteEventsSyntax SendCommands(Action<SendCommandPipe> opt)
        {
            if(_writeSyntax != null) throw new InvalidOperationException("ReceiveCommand has already been invoked.");
            if(opt == null) throw new ArgumentNullException(nameof(opt));

            _option = opt;
            return (_writeSyntax = new WriteEventsSyntax(_serviceProvider, _schemaFilter));
        }

        internal void Build()
        {
            if(_option == null || _writeSyntax == null) 
                throw new InvalidOperationException("ReceiveCommands was not configured.");

            _option(new SendCommandPipe(_serviceProvider, _schemaFilter));
            _writeSyntax.Build();
        }
    }
}