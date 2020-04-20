using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prism.Events;
using Serilog;

namespace EventDrivenThinking.App.Configuration
{
    public class RestCommandsSliceStartup : ICommandsSliceStartup
    {
        private IClientCommandSchema[] _commands;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var e in _commands)
            {
                var invokerType = typeof(HttpCommandInvoker<>).MakeGenericType(e.Type);
                var interfaceType = typeof(ICommandInvoker<>).MakeGenericType(e.Type);

                serviceCollection.TryAddScoped(interfaceType, invokerType);
            }
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            var aggregator = serviceProvider.GetRequiredService<IEventAggregator>();
            var dispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            foreach (var e in _commands)
            {
                logger.Information("Invocation of {CommandName} is configured to use http.", e.Type.Name);
                var configuratorType = typeof(AggregateConfigurator<>).MakeGenericType(e.Type);
                var configurator = Ctor<IAggregateConfigurator>.Create(configuratorType);
                configurator.Configure(aggregator, dispatcher);
            }

            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IClientCommandSchema> commandSchema)
        {
            this._commands = commandSchema.ToArray();
        }


        private interface IAggregateConfigurator
        {
            void Configure(IEventAggregator aggregator, ICommandDispatcher dispatcher);
        }

        private class AggregateConfigurator<TCommand> : IAggregateConfigurator where TCommand : EventDrivenThinking.EventInference.Abstractions.Write.ICommand
        {
            public void Configure(IEventAggregator aggregator, ICommandDispatcher dispatcher)
            {
                aggregator.GetEvent<PubSubEvent<CommandEnvelope<Guid, TCommand>>>()
                    .Subscribe(ev =>
                    {
                        dispatcher.Dispatch(ev.Id, ev.Command).GetAwaiter().GetResult();
                        // Here we need to connect to REST API!
                    }, ThreadOption.BackgroundThread, true);
            }
        }
    }
}