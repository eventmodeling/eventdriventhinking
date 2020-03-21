using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    /// <summary>
    /// Understands how to process events that related to this TModel
    /// Understands how to switch partitions. 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
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
            Debug.WriteLine($"QueryEngine for model {typeof(TModel).Name} created.");
        }

        public TModel CreateOrGet()
        {
            if (_model == null)
            {
                _model = ActivatorUtilities.GetServiceOrCreateInstance<TModel>(_serviceProvider);
                Debug.WriteLine($"Model {typeof(TModel).Name} created from serviceProvider.");
            }
            return _model;
        }

        public IQueryResult<TModel, TResult> Execute<TQuery, TResult>(TQuery query, QueryOptions options = null)
            where TQuery : IQuery<TModel, TResult>
        {
            // so here we read data from stream
            // so we request subscription from EventHub
            // the event hub will send it with the request id 
            // than this needs to be dispatched here
            // and when need events fall into this partition
            // we will get notifications since we are subscribed to the stream
            // so we need to buffer current events untill all the rest is processed
            // when we subscribe to partition
            // we might do it on 2 views - sharing one model because the switch won't delete cached data.
            // we also don't have to share model.
            // the problem is when partitions overlap. 
            // this can be solved:
            // - since we subscribe with a method name we know to which projection we fall, we also know what stream 
            // we subscribed, since we know the query. so at the place of subscription we need to collect this information


            var queryResult = new QueryResult<TQuery, TModel, TResult>(CreateOrGet(), query, options);
            var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TModel, TResult>>();

            _subscriber.SubscribeModelForQuery(_model, query, async events =>
            {
                _logger.Information("Executing projection for query {queryName} for model {modelName}", typeof(TQuery).Name, typeof(TModel).Name);
                var executor = _serviceProvider.GetService<IProjectionExecutor<TModel>>();
                executor.Configure(_model);
                await executor.Execute(events);
            });
            //queryResult.OnComplete();
            //onComplete(handler.Execute(_model, query));

            return queryResult;
        }

        
    }
}
