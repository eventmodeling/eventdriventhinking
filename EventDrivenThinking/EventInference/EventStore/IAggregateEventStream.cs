using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.EventStore
{
    

    public interface IAggregateEventStream<TAggregate>
    {
        IAsyncEnumerable<IEvent> Get(Guid key);
        
        //TODO: Need to add version here
        Task<EventEnvelope[]> Append(Guid key, ulong version, Guid correlationId, IEnumerable<IEvent> published);
        Task<EventEnvelope[]> Append(Guid key, Guid correlationId, IEnumerable<IEvent> published);
    }
}