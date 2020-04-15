using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IProcessorSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProcessorSchema schema,
            IEventHandlerFactory factory,
            Predicate<ISubscriptionProvider<IProcessor, IProcessorSchema>> filter = null,
            params object[] args);
    }
}