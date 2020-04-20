using EventDrivenThinking.EventInference.Abstractions;
using Prism.Events;

namespace EventDrivenThinking.EventInference.CommandHandlers
{
    public static class AggregatorExtensions
    {
        public static void Publish<T>(this IEventAggregator ea, T ev)
            where T : IEvent
        {
            ea.GetEvent<PubSubEvent<T>>().Publish(ev);
        }
    }
}