using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
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
        private readonly IEventAggregator _aggregator;
        private List<EventEnvelope> _events;

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
                .Subscribe(ev => _events.Add(ev), ThreadOption.UIThread, true);
        }
    }
    public class EventAggregatorModelProjectionSubscriber<TModel> : IModelProjectionSubscriber<TModel>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IEventStream _stream;
        private readonly ILogger _logger;
        private readonly MethodInfo _method;
        public EventAggregatorModelProjectionSubscriber(IEventAggregator eventAggregator, IEventStream stream, ILogger logger)
        {
            _eventAggregator = eventAggregator;
            _stream = stream;
            _logger = logger;
            _method = this.GetType().GetMethod(nameof(SubscribeCore), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Subscribe(IEnumerable<Type> eventTypes, bool fromBeginning,
            Action<IEnumerable<EventEnvelope>> onEventReceived)
        {
            var types = eventTypes.ToArray();
            foreach (var t in types)
                _method.MakeGenericMethod(t).Invoke(this, new object[] { onEventReceived });

            if (fromBeginning)
            {
                onEventReceived(_stream.GetEvents(types));
            }
        }

        private void SubscribeCore<TEventType>(Action<IEnumerable<EventEnvelope>> onEventReceived) where TEventType : IEvent
        {
            _logger.Information("SubscriptionManager is subscribing an event {eventName} though EventAggregator", typeof(TEventType).Name);
            _eventAggregator.GetEvent<PubSubEvent<EventEnvelope<TEventType>>>()
                .Subscribe((ev => onEventReceived(new[] {ev})), ThreadOption.UIThread,true);
        }


        public Task<ISubscription> SubscribeToStream(Func<(EventMetadata, IEvent)[], Task> onEvents, Action<ISubscription> liveProcessingStarted, Guid? partitionId = null, long? location = null)
        {
            throw new NotImplementedException();
        }
    }
}
