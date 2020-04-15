using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IProjectionStreamSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProjectionSchema schema, 
            IEventHandlerFactory factory,
            Predicate<ISubscriptionProvider<IProjectionEventStream, IProjectionSchema>> filter = null,
            params object[] args);
    }
}