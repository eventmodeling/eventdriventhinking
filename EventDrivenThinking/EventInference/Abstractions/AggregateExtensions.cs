using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public static class AggregateExtensions
    {
        public static void Rehydrate(this IAggregate aggregate, params IEvent[] events)
        {
            aggregate.Rehydrate(events);
        }
    }
}