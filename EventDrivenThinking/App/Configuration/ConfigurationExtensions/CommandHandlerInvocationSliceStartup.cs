﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration
{
    public class CommandHandlerInvocationSliceStartup : ICommandsSliceStartup
    {
        public CommandHandlerInvocationSliceStartup()
        {
            
        }
        private IEnumerable<IClientCommandSchema> _commands;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var i in _commands)
            {
                var interfaceType = typeof(ICommandInvoker<>).MakeGenericType(i.Type);
                var commandHandlerType = typeof(CommandHandlerInvoker<>).MakeGenericType(i.Type);
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