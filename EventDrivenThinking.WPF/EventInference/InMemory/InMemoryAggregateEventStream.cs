using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Reflection;
using Newtonsoft.Json;
using Prism.Events;
using Serilog;
using Serilog.Events;

#pragma warning disable 1998

namespace EventDrivenThinking.EventInference.InMemory
{
    public class OptimisticConcurrencyException : Exception { }

    public class InMemoryAggregateEventStream<TAggregate> : IAggregateEventStream<TAggregate>
    {
        private readonly IEventAggregator _eventAggregator;
        private IEventMetadataFactory<TAggregate> _metadataFactory;
        private readonly ConcurrentDictionary<object, List<EventEnvelope>> _streams;
        private ILogger _logger;

        public InMemoryAggregateEventStream(IEventAggregator eventAggregator, IEventMetadataFactory<TAggregate> metadataFactory, ILogger logger)
        {
            _eventAggregator = eventAggregator;
            _metadataFactory = metadataFactory;
            _logger = logger;
            _streams = new ConcurrentDictionary<object, List<EventEnvelope>>();
        }

        public async IAsyncEnumerable<IEvent> Get(Guid key)
        {
            var result = _streams.GetOrAdd(key, k => new List<EventEnvelope>());
            foreach (var i in result)
                yield return i.Event;
        }

        public async Task<EventEnvelope[]> Append(Guid key, Guid correlationId,
            IEnumerable<IEvent> published)
        {
            var list = _streams.GetOrAdd(key, k => new List<EventEnvelope>());
            uint i = 0;
            var result = published.Select(e =>
                    new EventEnvelope(e, _metadataFactory.Create(key, correlationId, e, 0)))
                .ToArray();

            lock (list)
            {
                list.AddRange(result);
            }
            LogEvents(key, 0, result);
            foreach (var e in result)
            {
                var invoker = Ctor<IInvoker>.Create(typeof(Invoker<>).MakeGenericType(typeof(TAggregate), e.Event.GetType()));
                invoker.Invoke(_eventAggregator, e.Metadata, e.Event);
            }

            return result;
        }
        public async Task<EventEnvelope[]> Append(Guid key, ulong version, Guid correlationId, IEnumerable<IEvent> published)
        {
            var list = _streams.GetOrAdd(key, k => new List<EventEnvelope>());
            uint i = 0;
            var result = published.Select(e =>
                new EventEnvelope(e, _metadataFactory.Create(key,correlationId,e, version+i++)))
                .ToArray();

            lock (list)
            {
                if ((uint)list.Count == (version+1))
                    list.AddRange(result);
                else throw new OptimisticConcurrencyException();
            }
            LogEvents(key, version, result);
            foreach (var e in result)
            {
                var invoker = Ctor<IInvoker>.Create(typeof(Invoker<>).MakeGenericType(typeof(TAggregate), e.Event.GetType()));
                invoker.Invoke(_eventAggregator,e.Metadata, e.Event);
            }

            return result;
        }

        private void LogEvents(Guid key, ulong version, EventEnvelope[] result)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug))
                for (ulong index = 0; index < (ulong)result.Length; index++)
                {
                    var i = result[index];
                    _logger.Debug("{aggregateName}-{id}@{version}\t{eventName}\t{data}",
                        typeof(TAggregate).Name,
                        key.ToString(), // we don't want "
                        version + index + 1,
                        i.Event.GetType().Name,
                        JsonConvert.SerializeObject(i.Event));
                }
        }

        private interface IInvoker
        {
            void Invoke(IEventAggregator aggregator, EventMetadata m, IEvent ev);
        }

        private class Invoker<T> : IInvoker
            where T : IEvent
        {
            public void Invoke(IEventAggregator aggregator, EventMetadata m, IEvent ev)
            {
                aggregator.GetEvent<PubSubEvent<EventEnvelope<T>>>().Publish(
                    new EventEnvelope<T>((T) ev, m));
            }
        }
    }
}