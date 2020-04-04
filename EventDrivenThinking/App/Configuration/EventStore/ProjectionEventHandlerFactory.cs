using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class ProjectionEventHandlerFactory : EventHandlerFactoryBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectionSchema _schema;

        public ProjectionEventHandlerFactory(IServiceProvider serviceProvider, IProjectionSchema schema)
        {
            _serviceProvider = serviceProvider;
            _schema = schema;
            SupportedEventTypes = new TypeCollection(_schema.Events);
        }

        public override TypeCollection SupportedEventTypes { get; }
        public override IEventHandler<TEvent> CreateHandler<TEvent>()
        {
            var type = typeof(ProjectionEventHandler<,>).MakeGenericType(_schema.Type, typeof(TEvent));
            return (IEventHandler<TEvent>)ActivatorUtilities.CreateInstance(_serviceProvider,type);
        }
    }
}