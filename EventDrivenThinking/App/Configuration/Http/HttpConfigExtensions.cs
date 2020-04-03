using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Http
{
    public static class HttpConfigExtensions
    {
        public static FeaturePartition UseHttp(this CommandsConfig config)
        {
            return config.Merge(new RestCommandsStartup());
        }
    }

    public class RestCommandsStartup : ICommandsSliceStartup
    {
        private IEnumerable<IClientCommandSchema> _commands;
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _commands)
            {
                var interfaceType = typeof(ICommandInvoker<>).MakeGenericType(i.Type);
                var commandHandlerType = typeof(HttpCommandInvoker<>).MakeGenericType(i.Type);
                serviceCollection.AddSingleton(interfaceType, commandHandlerType);
            }
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IClientCommandSchema> commandSchema)
        {
            this._commands = commandSchema;
        }
    }
}
