using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Serilog;

namespace EventDrivenThinking.App.Configuration.Fresh.EventAggregator
{
    public class CommandHandlerInvocationSliceStartup : ICommandInvocationSliceStartup
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var e in _commands)
            {
                var invokerType = typeof(CommandHandlerInvoker<>).MakeGenericType(e.Type);
                var interfaceType = typeof(ICommandInvoker<>).MakeGenericType(e.Type);

                serviceCollection.AddScoped(interfaceType, invokerType);

            }
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            var aggregator = serviceProvider.GetRequiredService<IEventAggregator>();
            var dispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            foreach (var e in _commands)
            {
                logger.Information("Invocation of {CommandName} is configured to use local command-handler.", e.Type.Name);
                var configuratorType = typeof(AggregateConfigurator<>).MakeGenericType(e.Type);
                var configurator = Ctor<IAggregateConfigurator>.Create(configuratorType);
                configurator.Configure(aggregator, dispatcher, logger);
            }
            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IClientCommandSchema> commandSchema)
        {
            this._commands = commandSchema.ToArray();
        }
        private IClientCommandSchema[] _commands;


        private interface IAggregateConfigurator
        {
            void Configure(IEventAggregator aggregator, ICommandDispatcher commandDispatcher, ILogger logger);
        }

        private class AggregateConfigurator<TCommand> : IAggregateConfigurator where TCommand : ICommand
        {
            public void Configure(IEventAggregator aggregator,
                ICommandDispatcher commandDispatcher, ILogger logger)
            {
                aggregator.GetEvent<PubSubEvent<CommandEnvelope<Guid, TCommand>>>()
                    .Subscribe(ev =>
                    {
                        commandDispatcher.Dispatch(ev.Id, ev.Command).GetAwaiter().GetResult();
                    }, ThreadOption.PublisherThread, true);
                logger.Information("Subscribed to {commandName}", typeof(TCommand).Name);
            }
        }
    }
}