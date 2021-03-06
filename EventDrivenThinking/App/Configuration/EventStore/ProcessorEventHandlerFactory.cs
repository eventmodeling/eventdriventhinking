﻿using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProcessorEventHandlerFactory : EventHandlerFactoryBase
    {
        private readonly IProcessorSchema _schema;

        public ProcessorEventHandlerFactory(IServiceProvider serviceProvider, IProcessorSchema schema) : base(serviceProvider)
        {

            _schema = schema;
            SupportedEventTypes = new TypeCollection(_schema.Events);
        }

        public override TypeCollection SupportedEventTypes { get; }
        protected override IEventHandler<TEvent> CreateHandler<TEvent>(IServiceScope scope)
        {
            var type = typeof(ProcessorEventHandler<,>).MakeGenericType(_schema.Type, typeof(TEvent));
            return (IEventHandler<TEvent>)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type);
        }
    }
}