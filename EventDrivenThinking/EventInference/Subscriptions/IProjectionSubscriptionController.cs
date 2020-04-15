using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IProjectionSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProjectionSchema schema, 
            IEventHandlerFactory factory,
            Predicate<ISubscriptionProvider<IProjection, IProjectionSchema>> filter = null,
            params object[] args);
    }
}