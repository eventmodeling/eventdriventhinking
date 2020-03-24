using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui;
using EventDrivenThinking.Utils;
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
        class ProjectionStreamInfo
        {
            public Guid? PartitionId { get; private set; }
            public bool IsRootStream => PartitionId.HasValue;
            public ISubscription Subscription { get; set; }
            public readonly ConcurrentDictionary<IQuery, ILiveQuery> AssociatedQueries;
            public ProjectionStreamInfo(Guid? partitionId)
            {
                PartitionId = partitionId;
                AssociatedQueries = new ConcurrentDictionary<IQuery, ILiveQuery>();
            }
        }
        internal class LiveQuery<TQuery, TResult> : ILiveQuery, ILiveResult<TResult>
            where TQuery: IQuery<TModel, TResult>
        {
            private readonly Action<TQuery> _onDispose;
            
            IQuery ILiveQuery.Query => Query;
            public LiveQueryStatus Status { get; private set; }
            object ILiveResult.Result => Result;

            public event EventHandler ResultUpdated;
            public event EventHandler StatusChanged;
            object ILiveQuery.Result => Result;
            
            public QueryOptions Options { get; }
            
            public TQuery Query { get; }
            public TResult Result { get; private set; }
            public Guid? PartitionId { get; }
            public LiveQuery(TQuery query, Guid? partitionId,
                IQuerySchema schema,
                Action<TQuery> onDispose,
                QueryOptions options)
            {
                _onDispose = onDispose;
                Query = query;
                Options = options;
                PartitionId = partitionId;
            }

            public void OnResult(TResult result)
            {
                this.Result = result;
                Status = LiveQueryStatus.Running;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }

            public void OnUpdate(TResult result)
            {
                this.Result.CopyFrom(result);
                ResultUpdated?.Invoke(this, EventArgs.Empty);
            }

            public void Dispose()
            {
                _onDispose(this.Query);
                Status = LiveQueryStatus.Disposed;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        interface ILiveQuery
        {
            IQuery Query { get; }
            object Result { get; }
            //IQueryResult QueryResult { get; }
            Guid? PartitionId { get; }
            QueryOptions Options { get; }
        }

        private ConcurrentDictionary<IQuery, ILiveQuery> _liveQueries;
        private ConcurrentDictionary<Guid,ProjectionStreamInfo> _partitions;

        private TModel _model;
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuerySchemaRegister _querySchemaRegister;
        private readonly IProjectionSchemaRegister _projectionSchemaRegister;

        private readonly IModelProjectionSubscriber<TModel> _modelProjectionSubscriber;
        private readonly ILogger _logger;

        public QueryEngine(IServiceProvider serviceProvider, 
            IQuerySchemaRegister _querySchemaRegister,
            IProjectionSchemaRegister _projectionSchemaRegister,
            IModelProjectionSubscriber<TModel> modelProjectionSubscriber, 
            ILogger logger)
        {
            _serviceProvider = serviceProvider;
            this._querySchemaRegister = _querySchemaRegister;
            this._projectionSchemaRegister = _projectionSchemaRegister;
            _modelProjectionSubscriber = modelProjectionSubscriber;
            _logger = logger;
            _liveQueries = new ConcurrentDictionary<IQuery, ILiveQuery>();
            _partitions = new ConcurrentDictionary<Guid, ProjectionStreamInfo>();
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
        

        public async Task<ILiveResult<TResult>> Execute<TQuery, TResult>(TQuery query, QueryOptions options = null)
            where TQuery : IQuery<TModel, TResult>
            where TResult: class
        {
            var schema = _querySchemaRegister.GetByQueryType(typeof(TQuery));
            var projectionSchema = _projectionSchemaRegister.FindByModelType(typeof(TModel));

            var queryParitioner =  _serviceProvider.GetService<IQueryPartitioner<TQuery>>();
            
            var partitionId = queryParitioner?.CalculatePartition(CreateOrGet(), query);
            
            LiveQuery<TQuery, TResult> liveQuery = new LiveQuery<TQuery,  TResult>(query, partitionId, schema, OnQueryDispose, options);

            if (partitionId.HasValue)
            {
                if (!_partitions.TryGetValue(partitionId.Value, out ProjectionStreamInfo streamInfo))
                {
                    streamInfo = new ProjectionStreamInfo(partitionId);
                    // this needs to wait for all events to go though the wire.
                    AsyncAutoResetEvent subscriptionReady = new AsyncAutoResetEvent(false);
                    streamInfo.Subscription = await _modelProjectionSubscriber.SubscribeToStream(
                        async events => await OnEvents<TQuery, TResult>(streamInfo, schema.ProjectionType, events),
                        subscription => { subscriptionReady.Set(); },
                        partitionId.Value);
                    
                    _partitions.TryAdd(partitionId.Value, streamInfo);

                    subscriptionReady.Wait();
                    Debug.WriteLine("Waiting for projection to kick in...");
                    Thread.Sleep(2000);
                }
                
                // we have subscribed to partition before. Just need to return result and execute the handler.
                var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TModel, TResult>>();
                var result = handler.Execute(CreateOrGet(), query);
                liveQuery.OnResult(result);

                streamInfo.AssociatedQueries.TryAdd(liveQuery.Query, liveQuery);
                _liveQueries.TryAdd(liveQuery.Query, liveQuery);
                
            }
            else
            {
                throw new NotImplementedException();
            }

            
            

            return liveQuery;
        }

        private void OnQueryDispose<TQuery>(TQuery obj) where TQuery : IQuery
        {
            if (_liveQueries.TryRemove(obj, out ILiveQuery liveQuery))
            {
                if (liveQuery.PartitionId.HasValue)
                    _partitions[liveQuery.PartitionId.Value].AssociatedQueries.TryRemove(liveQuery.Query, out ILiveQuery tmp);
            }
        }

        async Task OnEvents<TQuery, TResult>(ProjectionStreamInfo streamInfo, Type projectionType, IEnumerable<(EventMetadata, IEvent)> ev)
        where TQuery:IQuery<TModel, TResult>
        {
            Debug.WriteLine($"OnEvent in QueryEngine. Partition {streamInfo.PartitionId}");
            var projection = (IProjection<TModel>) ActivatorUtilities.CreateInstance(_serviceProvider, projectionType, CreateOrGet());
            await projection.Execute(ev);
            
            foreach (LiveQuery<TQuery, TResult> i in streamInfo.AssociatedQueries.Values.OfType<LiveQuery<TQuery, TResult>>())
            {
                Debug.WriteLine("Found live query to update.");
                var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TModel, TResult>>();
                var result = handler.Execute(CreateOrGet(), i.Query);

                i.OnUpdate(result);
            }
        }

        
    }
}
