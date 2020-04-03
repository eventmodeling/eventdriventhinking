using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class MultiEventSubscriptionProvider : ISubscriptionProvider<IProjection>
    {
        readonly List<SingleEventSubscriptionProvider> _providers;

        public MultiEventSubscriptionProvider(SingleEventSubscriptionProvider singleEventSubscription)
        {
            _providers = new List<SingleEventSubscriptionProvider>(){ singleEventSubscription };
        }

        public virtual bool CanMerge(ISubscriptionProvider<IProjection> other)
        {
            return other is MultiEventSubscriptionProvider || other is SingleEventSubscriptionProvider;
        }

        public virtual ISubscriptionProvider<IProjection> Merge(ISubscriptionProvider<IProjection> other)
        {
            if (other is MultiEventSubscriptionProvider mp)
            {
                _providers.AddRange(mp._providers);
            }
            else
            {
                _providers.Add(other as SingleEventSubscriptionProvider);
            }
            return this;
        }

        public virtual async Task Subscribe(ISchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            IProjectionSchema pSchema = (IProjectionSchema)schema;



        }
    }
}