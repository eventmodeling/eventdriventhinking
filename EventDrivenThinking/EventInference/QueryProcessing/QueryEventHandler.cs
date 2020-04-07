using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Ui;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    class QueryEventHandler<TEvent> : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        private readonly IEventHandler<TEvent> _projectionHandler;
        private readonly IModel _model;
        private readonly IEnumerable<ILiveQuery> _queries;
        private readonly DispatcherQueue _dispatcherQueue;

        internal QueryEventHandler(IEventHandler<TEvent> projectionHandler,
            IModel model, IEnumerable<ILiveQuery> queries, DispatcherQueue dispatcherQueue)
        {
            _projectionHandler = projectionHandler;
            _model = model;
            _queries = queries;
            _dispatcherQueue = dispatcherQueue;
        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            _dispatcherQueue.Enqueue(async () =>
            {
                await _projectionHandler.Execute(m, ev);
                foreach (var i in _queries)
                    i.Load(_model);
            });
        }
    }
}