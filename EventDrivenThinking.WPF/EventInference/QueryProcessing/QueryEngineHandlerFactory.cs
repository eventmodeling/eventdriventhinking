using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    class QueryEngineHandlerFactory<TModel> : EventHandlerFactoryBase
        where TModel : IModel
    {
        private readonly IProjectionSchema _schema;
        private readonly Func<TModel> _getModel;
        private readonly DataPartitionStream<TModel> _stream;
        private readonly Lazy<DispatcherQueue> _lazyDispatcher;
        internal QueryEngineHandlerFactory(IServiceProvider serviceProvider, 
            Func<TModel> getModel, 
            DataPartitionStream<TModel> stream, 
            IProjectionSchema schema) : base(serviceProvider)
        {
            _getModel = getModel;
            _stream = stream;
            _schema = schema;
            _lazyDispatcher = new Lazy<DispatcherQueue>(() =>
            {
                //return ActivatorUtilities.GetServiceOrCreateInstance<DispatcherQueue>(_serviceProvider);
                 return DispatcherQueue.Instance; 
            });
            SupportedEventTypes = new TypeCollection(_schema.Events);
        }

        public override TypeCollection SupportedEventTypes { get; }
        protected override IEventHandler<TEvent> CreateHandler<TEvent>(IServiceScope scope)
        {
            var projectionInstance = (IProjection)ActivatorUtilities.CreateInstance(scope.ServiceProvider, _schema.Type, _getModel());
            var handler = new ProjectionEventHandler<TEvent>(projectionInstance);
            return new QueryEventHandler<TEvent>(handler, _getModel(), _stream.Queries, _lazyDispatcher.Value);
        }
    }
}