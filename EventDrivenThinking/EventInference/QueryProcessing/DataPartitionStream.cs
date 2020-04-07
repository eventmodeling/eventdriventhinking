using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using Nito.AsyncEx;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    class DataPartitionStream<TModel> where TModel:IModel
    {
        public Guid? PartitionId { get; private set; }
        public bool IsRootStream { get; private set; }

        private readonly QueryEngine<TModel> _engine;
        private readonly IProjectionSubscriptionController _subscriptionController;
        private readonly ConcurrentDictionary<IQuery, ILiveQuery> _associatedQueries;
        
        public IEnumerable<ILiveQuery> Queries => _associatedQueries.Values;

        private readonly IProjectionSchema _schema;
        
        private ISubscription _subscription;
        private Task _initTask;
        public DataPartitionStream(Guid partitionId,
            QueryEngine<TModel> engine,
            IProjectionSubscriptionController subscriptionController,
            IProjectionSchema schema, 
            bool isRootPartition = false)
        {
            IsRootStream = isRootPartition;
            PartitionId = partitionId;
            _engine = engine;
            _subscriptionController = subscriptionController;
            _schema = schema;
            _associatedQueries = new ConcurrentDictionary<IQuery, ILiveQuery>();

            _initTask = Initialize();
        }
        
        async Task Initialize()
        {
            var handlerFactory = new QueryEngineHandlerFactory<TModel>(_engine.ServiceProvider, 
                () => _engine.GetModel(), 
                this, 
                _schema);

            if (!IsRootStream)
            {
                _subscription = await _subscriptionController.SubscribeHandlers(_schema, handlerFactory, PartitionId.Value);
            }
            else
                _subscription = await _subscriptionController.SubscribeHandlers(_schema, handlerFactory);
        }

        public void AppendQuery<TQuery, TResult>(LiveQuery<TQuery, TModel, TResult> liveQuery) where TQuery : IQuery<TModel, TResult> where TResult : class
        {
            if (_associatedQueries.TryAdd(liveQuery.Query, liveQuery))
            {
                liveQuery.Load(_engine.GetModel());
            }
        }

        public void RemoveQuery(IQuery query)
        {
            _associatedQueries.TryRemove(query, out var value);
        }

        public async Task Catchup()
        {
            await _initTask;
            if (_subscription != null)
                await _subscription.Catchup();
        }
    }
}