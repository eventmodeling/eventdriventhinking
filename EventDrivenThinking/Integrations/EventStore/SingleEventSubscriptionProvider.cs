using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;

namespace EventDrivenThinking.Integrations.EventStore
{
    public abstract class SingleEventSubscriptionProvider : ISubscriptionProvider<IProjection>
    {
        public abstract Type EventType { get; }
        public virtual bool CanMerge(ISubscriptionProvider<IProjection> other)
        {
            return other is SingleEventSubscriptionProvider || other is MultiEventSubscriptionProvider;
        }

        public virtual ISubscriptionProvider<IProjection> Merge(ISubscriptionProvider<IProjection> other)
        {
            if (other is MultiEventSubscriptionProvider multiProvider)
            {
                return other.Merge(this);
            }
            else return new MultiEventSubscriptionProvider(this);
        }

        public abstract Task Subscribe(ISchema schema, IEventHandlerFactory factory, object[] args = null);

    }
}