using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IEventSubscriptionProvider<TOwner, TSchema, TEvent> : 
        ISubscriptionProvider<TOwner, TSchema>
        where TSchema : ISchema
        where TEvent : IEvent
    {
        
    }
}