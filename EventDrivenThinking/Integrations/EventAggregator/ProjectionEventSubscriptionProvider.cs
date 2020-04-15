using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using Prism.Events;


namespace EventDrivenThinking.Integrations.EventAggregator
{
    [DebuggerDisplay("EventAggregator Subscriber, {EventType.Name}")]
    public class ProjectionEventSubscriptionProvider<TEvent> : SingleEventSubscriptionProvider,
        IEventSubscriptionProvider<IProjection, IProjectionSchema, TEvent>
        where TEvent : IEvent
    {
        
        public override Type EventType => typeof(TEvent);
        
        public override async Task<ISubscription> Subscribe( IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription(true);
            if (!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            _eventAggregator.GetEvent<PubSubEvent<EventEnvelope<TEvent>>>().Subscribe(e =>
            {
                if (e.Event.GetType() == typeof(TEvent))
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler<TEvent>();

                        handler.Execute(e.Metadata, (TEvent)e.Event).GetAwaiter().GetResult();
                    }
                }
            }, ThreadOption.UIThread, true);
            
            return s;
        }

        public ProjectionEventSubscriptionProvider(IEventConverter eventConverter, IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
}
