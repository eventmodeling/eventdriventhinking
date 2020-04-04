using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;

namespace EventDrivenThinking.Integrations.EventStore
{
    public abstract class SingleEventSubscriptionProvider : 
        ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        protected readonly IEventStoreFacade _eventStore;
        protected readonly IEventConverter _eventConverter;

        protected SingleEventSubscriptionProvider(IEventStoreFacade eventStore, IEventConverter eventConverter)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
        }

        public abstract Type EventType { get; }
        public virtual bool CanMerge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            return other is SingleEventSubscriptionProvider || other is MultiEventSubscriptionProvider;
        }

        public virtual ISubscriptionProvider<IProjection, IProjectionSchema> Merge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            if (other is MultiEventSubscriptionProvider multiProvider)
            {
                return other.Merge(this);
            }
            else return new MultiEventSubscriptionProvider(this, _eventStore,_eventConverter);
        }

        public abstract Task Subscribe(IProjectionSchema schema, IEventHandlerFactory factory, object[] args = null);

    }
}