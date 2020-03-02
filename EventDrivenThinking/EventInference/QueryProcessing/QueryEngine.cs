using System;
using System.Collections.Generic;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.Ui;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    public class QueryEngine<TModel> : IQueryEngine<TModel> where TModel : IModel
    {
        private TModel _model;
        private readonly IServiceProvider _serviceProvider;
        
        private readonly IModelSubscriber<TModel> _subscriber;
        private readonly ILogger _logger;

        public QueryEngine(IServiceProvider serviceProvider,
            IModelSubscriber<TModel> subscriber, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            

            _subscriber = subscriber;
            _logger = logger;
        }

        public TModel CreateOrGet()
        {
            if (_model == null)
                _model = ActivatorUtilities.GetServiceOrCreateInstance<TModel>(_serviceProvider);
            return _model;
        }


        public void Subscribe<TQuery, TResult>(TQuery query, Action<TResult> onComplete) where TQuery : IQuery<TModel, TResult>
        {
            _logger.Information("QueryEngine is subscribing for {modelName}", typeof(TModel).Name);
            var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TModel, TResult>>();

            _subscriber.SubscribeModelForQuery(_model, query, async events =>
            {
                _logger.Information("Executing projection for query {queryName} for model {modelName}", typeof(TQuery).Name, typeof(TModel).Name);
                var executor = _serviceProvider.GetService<IProjectionExecutor<TModel>>();
                executor.Configure(_model);
                await executor.Execute(events);
            });
            onComplete(handler.Execute(_model, query));
        }
    }
}
