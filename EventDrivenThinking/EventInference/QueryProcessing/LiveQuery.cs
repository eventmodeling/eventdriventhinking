using System;
using System.Diagnostics;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Schema;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    class LiveQuery<TQuery,TModel, TResult> : ILiveQuery, ILiveResult<TResult>
        where TQuery : IQuery<TModel, TResult>
        where TModel : IModel
    {
        private readonly IQueryHandler<TQuery, TModel, TResult> _handler;
        private readonly Action<TQuery> _onDispose;

        IQuery ILiveQuery.Query => Query;
        public LiveQueryStatus Status { get; private set; }
        object ILiveResult.Result => Result;

        private EventHandler _resultUpdated;
        public event EventHandler ResultUpdated
        {
            add
            {
                lock (this)
                {
                    _resultUpdated += value;
                    if(Status == LiveQueryStatus.Running)
                        _resultUpdated(this, EventArgs.Empty);
                }
            }
            remove
            {
                _resultUpdated -= value;
            }
        }
        public event EventHandler StatusChanged;
        object ILiveQuery.Result => Result;

        public QueryOptions Options { get; }

        public TQuery Query { get; }
        public TResult Result { get; private set; }
        public Guid? PartitionId { get; }
        public LiveQuery(TQuery query, Guid? partitionId,
            IQueryHandler<TQuery, TModel, TResult> handler,
            IQuerySchema schema,
            Action<TQuery> onDispose,
            QueryOptions options)
        {
            _handler = handler;
            _onDispose = onDispose;
            Query = query;
            Options = options;
            PartitionId = partitionId;
        }

        public void Load(IModel model)
        {
            var result = _handler.Execute((TModel)model, Query);
            if (Status == LiveQueryStatus.Initialized)
                OnResult(result);
            else OnUpdate(result);
        }
        internal void OnResult(TResult result)
        {
            if (result == null && typeof(TResult).Name == "Board")
                Debugger.Break();

            
            this.Result = result;
            lock (this)
            {
                Status = LiveQueryStatus.Running;
                StatusChanged?.Invoke(this, EventArgs.Empty);
                _resultUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void OnUpdate(TResult result)
        {
            if(result == null)
                Debugger.Break();
            this.Result.CopyFrom(result);
            lock (this)
            {
                _resultUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _onDispose(this.Query);
            Status = LiveQueryStatus.Disposed;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}