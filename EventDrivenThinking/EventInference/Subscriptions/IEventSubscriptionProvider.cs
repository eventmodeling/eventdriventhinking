using EventDrivenThinking.EventInference.Abstractions;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IEventSubscriptionProvider<TOwner, TEvent> : ISubscriptionProvider<TOwner>
        where TEvent : IEvent
    {
        
    }
}