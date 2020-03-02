using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;

namespace EventDrivenThinking.EventInference.EventStore
{
    public static class EventStreamExtensions
    {
        public static async Task Append<TAggregate>(this IAggregateEventStream<TAggregate> stream,
             Guid key, long version, Guid correlationId,
            params IEvent[] published)
        {
            await stream.Append(key, version, correlationId, published);
        }
    }
}