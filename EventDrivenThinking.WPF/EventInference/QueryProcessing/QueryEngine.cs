using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Logging;
using EventDrivenThinking.Ui;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;

using Serilog;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    /// <summary>
    /// Understands how to process events that related to this TModel
    /// Understands how to switch partitions.
    /// Can execute many queries.
    /// Under the hood have one model.
    /// If model changes, query results changes automatically
    /// When two queries are executed, both have results, both will get updates.
    /// Query Engine tracks query-results and how they will be affected by new events.
    /// If Query returns data from the model, then changes also will be propagated.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>l
    public class QueryEngine<TModel> : IQueryEngine<TModel> where TModel : IModel
    {
        private static readonly ILogger Log = LoggerFactory.For<QueryEngine<TModel>>();

        private readonly ConcurrentDictionary<IQuery, ILiveQuery> _liveQueries;
        private readonly ConcurrentDictionary<Guid,DataPartitionStream<TModel>> _partitions;

        
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuerySchemaRegister _querySchemaRegister;
        private readonly IProjectionSchemaRegister _projectionSchemaRegister;
        private readonly IProjectionSubscriptionController _subscriptionController;
        
        private readonly Guid _rootPartitionId = Guid.NewGuid();

        private TModel _model;

        internal IServiceProvider ServiceProvider => _serviceProvider;

        public QueryEngine(IServiceProvider serviceProvider, 
            IQuerySchemaRegister querySchemaRegister,
            IProjectionSchemaRegister projectionSchemaRegister,
            IProjectionSubscriptionController subscriptionController)
        {
            _serviceProvider = serviceProvider;
            _querySchemaRegister = querySchemaRegister;
            _projectionSchemaRegister = projectionSchemaRegister;
            _subscriptionController = subscriptionController;
        

            _liveQueries = new ConcurrentDictionary<IQuery, ILiveQuery>();
            _partitions = new ConcurrentDictionary<Guid, DataPartitionStream<TModel>>();
            
            Log.Debug("QueryEngine for model {modelName} created.", typeof(TModel).Name);
        }

        public TModel GetModel()
        {
            if (_model == null)
            {
                lock (this)
                {
                    if(_model == null)
                    {
                        if (typeof(TModel).IsInterface)
                            _model = ActivatorUtilities.GetServiceOrCreateInstance<TModel>(_serviceProvider);
                        else
                            _model = ViewModelFactory<TModel>.Create(_serviceProvider);
                    }
                }
            }
            return _model;
        }


        
        public async Task<ILiveResult<TResult>> Execute<TQuery, TResult>(TQuery query, QueryOptions options = null)
            where TQuery : IQuery<TModel, TResult>
            where TResult: class
        {
            var schema = _querySchemaRegister.GetByQueryType(typeof(TQuery));
            var projectionSchema = _projectionSchemaRegister.FindByModelType(typeof(TModel));

            var queryHandler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TModel, TResult>>();
            var queryParitioner =  GetQueryPartitioner<TQuery, TResult>();
            
            var partitionId = queryParitioner?.CalculatePartition(query);

            LiveQuery<TQuery,TModel, TResult> liveQuery = new LiveQuery<TQuery, TModel,  TResult>(query, partitionId, queryHandler, schema, OnQueryDispose, options);

            var streamInfo = _partitions.GetOrAdd(partitionId.HasValue ? partitionId.Value : _rootPartitionId, pid => new DataPartitionStream<TModel>(pid, this, _subscriptionController, projectionSchema, !partitionId.HasValue));
            
            await streamInfo.Catchup();
            
            streamInfo.AppendQuery(liveQuery);

            _liveQueries.TryAdd(liveQuery.Query, liveQuery);
            
            // HACK
            if (liveQuery.Options.ExpectNotNull)
            {
                while (liveQuery.Result == null)
                {
                    liveQuery.Load(GetModel());
                    await Task.Delay(100);
                }
            }

            return liveQuery;
        }

        private IQueryPartitioner<TQuery> GetQueryPartitioner<TQuery, TResult>() where TQuery : IQuery<TModel, TResult> where TResult: class
        {
            try
            {
                return _serviceProvider.GetService<IEnumerable<IQueryPartitioner<TQuery>>>()
                    .SingleOrDefault();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private void OnQueryDispose<TQuery>(TQuery obj) where TQuery : IQuery
        {
            if (_liveQueries.TryRemove(obj, out ILiveQuery liveQuery))
            {
                var id = liveQuery.PartitionId ?? _rootPartitionId;
                _partitions[id].RemoveQuery(liveQuery.Query);
            }
        }
        
        

        
    }
}
