using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using Prism.Events;

namespace EventDrivenThinking.Integrations.EventAggregator
{
    public abstract class SingleEventSubscriptionProvider :
        ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        protected readonly IEventAggregator _eventAggregator;
        
        protected IProjectionSchema _schema;

        public string Type => "EventAggregator";

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }
        protected SingleEventSubscriptionProvider( IEventAggregator eventAggregator)
        {
            
            _eventAggregator = eventAggregator;
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
            else return new MultiEventSubscriptionProvider(this,_eventAggregator, _schema);
        }

        public abstract Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null);

    }
}