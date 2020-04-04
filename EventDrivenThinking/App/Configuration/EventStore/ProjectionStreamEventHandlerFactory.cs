using System;
using System.Runtime.InteropServices;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProjectionStreamEventHandlerFactory : EventHandlerFactoryBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectionSchema _schema;

        public ProjectionStreamEventHandlerFactory(IServiceProvider serviceProvider, IProjectionSchema schema)
        {
            _serviceProvider = serviceProvider;
            _schema = schema;
            SupportedEventTypes = new TypeCollection(_schema.Events);
        }

        public override TypeCollection SupportedEventTypes { get; }
        public override IEventHandler<TEvent> CreateHandler<TEvent>()
        {
            var type = typeof(ProjectionStreamEventHandler<,>).MakeGenericType(_schema.Type, typeof(TEvent));
            return (IEventHandler<TEvent>)ActivatorUtilities.CreateInstance(_serviceProvider, type);
        }
    }
}