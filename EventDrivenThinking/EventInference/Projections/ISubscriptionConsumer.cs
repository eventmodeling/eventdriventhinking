using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Projections
{
    public interface ISubscriptionConsumer
    {
        Task Execute(IEnumerable<EventEnvelope> events);
    }
}