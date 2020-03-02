using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Abstractions.Write
{
    public interface IAggregate
    {
        long Version { get; }
        Guid Id { get; set; }
        void Rehydrate(IEnumerable<IEvent> events);
        Task RehydrateAsync(IAsyncEnumerable<IEvent> events);
        IEvent[] Execute(ICommand cmd);
    }
}