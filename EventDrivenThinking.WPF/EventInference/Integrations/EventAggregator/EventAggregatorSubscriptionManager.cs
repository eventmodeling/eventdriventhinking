using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Logging;
using Prism.Events;
using Serilog;

namespace EventDrivenThinking.Integrations.EventAggregator
{
    public interface IEventStream
    {
        IEnumerable<EventEnvelope> GetEvents(IEnumerable<Type> eventTypes);
    }
    public class InMemoryEventStream : IEventStream
    {
        private static readonly  ILogger Log = LoggerFactory.For<InMemoryEventStream>();
        private readonly IEventAggregator _aggregator;
        private readonly List<EventEnvelope> _events;


        public InMemoryEventStream(IEventAggregator aggregator, 
            IProjectionSchemaRegister projectionSchema)
        {
            _aggregator = aggregator;
            _events = new List<EventEnvelope>();
            var mth = this.GetType().GetMethod(nameof(SubscribeCore), BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var t in projectionSchema.Events)
                mth.MakeGenericMethod(t).Invoke(this, Array.Empty<object>());
        }

        public IEnumerable<EventEnvelope> GetEvents(IEnumerable<Type> eventTypes)
        {
            var filter = new HashSet<Type>(eventTypes);
            return _events.Where(x => filter.Contains(x.Event.GetType()));
        }
        private void SubscribeCore<TEventType>() where TEventType : IEvent
        {
            _aggregator.GetEvent<PubSubEvent<EventEnvelope<TEventType>>>()
                .Subscribe(ev =>
                {
                    _events.Add(ev);
                    Log.Debug("Saving in-memory event {eventName} for aggregate {aggregateName}", 
                        typeof(TEventType).Name, ev.Metadata.AggregateType.Name );
                }, ThreadOption.PublisherThread, true);
        }
    }
    public class EventAggregatorModelProjectionSubscriber<TModel,TProjection> : IModelProjectionSubscriber<TModel>
    {
        class EventAggregateSubscription : ISubscription
        {
            public readonly SubscriptionToken Token;

            public EventAggregateSubscription(SubscriptionToken token)
            {
                Token = token;
            }
        }
        private IProjectionSchema _schema;
        private PubSubEvent<ProjectionEvent<TProjection>> _pubSubEvent;

        public EventAggregatorModelProjectionSubscriber(IEventAggregator eventAggregator, 
            IProjectionSchemaRegister projectionSchemaRegister)
        {
            _pubSubEvent = eventAggregator.GetEvent<PubSubEvent<ProjectionEvent<TProjection>>>();
            _schema = projectionSchemaRegister.FindByModelType(typeof(TModel));
        }


        public async Task<ISubscription> SubscribeToStream(Func<(EventMetadata, IEvent)[], Task> onEvents, 
            Action<ISubscription> onLiveStarted = null, 
            Guid? partitionId = null, 
            long? location = null)
        {
            EventAggregateSubscription subscription = new EventAggregateSubscription(_pubSubEvent.Subscribe( x=> { 

                if(partitionId == x.PartitionId)
                    onEvents(new (EventMetadata, IEvent)[] {(x.Event.Metadata, x.Event.Event)}).GetAwaiter().GetResult();

            }, ThreadOption.UIThread,true));

            onLiveStarted?.Invoke(subscription);

            return subscription;
        }
        
    }
}
