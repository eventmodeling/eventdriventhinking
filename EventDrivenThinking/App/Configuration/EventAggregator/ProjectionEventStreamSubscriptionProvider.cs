using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Logging;
using EventDrivenThinking.Utils;
using Prism.Events;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    
    public abstract class SingleEventStreamSubscriptionProvider : ISubscriptionProvider<IProjectionEventStream, IProjectionSchema>
    {
        public string Type => "EventAggregator";
        protected IProjectionSchema _schema;
        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }
        public abstract Type EventType { get; }
        public abstract bool CanMerge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other);
        public abstract ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> Merge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other);
        public abstract Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null);
    }
    /// <summary>
    /// Subscribes to Event-Types. Is used for creating projection stream using a factory.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    [DebuggerDisplay("EventAggregator Stream Subscriber, {EventType.Name}")]
    public class ProjectionEventStreamSubscriptionProvider<TEvent> : SingleEventStreamSubscriptionProvider
        where TEvent : IEvent
    {
        private static ILogger Log = LoggerFactory.For<ProjectionEventStreamSubscriptionProvider<TEvent>>();
        
        private readonly IEventAggregator _eventAggregator;

        public ProjectionEventStreamSubscriptionProvider(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public override Type EventType => typeof(TEvent);

        
        public override bool CanMerge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            return false;
        }

        public override  ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> Merge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            throw new NotSupportedException();
        }

        public override async Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription(true);

            var token = _eventAggregator.GetEvent<PubSubEvent<EventEnvelope<TEvent>>>()
                .Subscribe( ev =>
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler<TEvent>();

                        handler.Execute(ev.Metadata, ev.Event).GetAwaiter().GetResult();
                    }
                }, ThreadOption.PublisherThread,true);
            Log.Debug("FactoryStream Handler subscribed to {eventType} for projection {projectionName}", typeof(TEvent).Name, _schema.Type.Name);
            return s;
        }
    }
}