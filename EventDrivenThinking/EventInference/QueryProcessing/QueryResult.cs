using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    public class QueryResult<TQuery, TModel, TResult> : IQueryResult<TModel, TResult>
        where TModel : IModel
        where TQuery : IQuery<TModel, TResult>
    {
        private readonly TModel _model;
        private readonly IQueryEngine<TModel> _engine;
        private readonly TQuery _query;
        private readonly QueryOptions _options;

        public QueryResult(TModel model,
            IQueryEngine<TModel> engine,
            TQuery query,
            QueryOptions options)
        {
            _model = model;
            _engine = engine;

            _query = query;
            _options = options;
        }

        public void Dispose()
        {
            // need to unsubscribe
        }

        public TResult Result { get; set; }
        public TModel Model => _model;
        private EventHandler _completed;
        public event EventHandler Completed
        {
            add
            {
                _completed += value; 
                if(IsCompleted)
                    value(this, EventArgs.Empty);
            }
            remove { _completed -= value; }
        }

        public bool IsCompleted { get; private set; }
        public void OnComplete(TResult result)
        {
            IsCompleted = true;
            Result = result;
            if(_completed != null)
                _completed(this, EventArgs.Empty);
        }
    }
}