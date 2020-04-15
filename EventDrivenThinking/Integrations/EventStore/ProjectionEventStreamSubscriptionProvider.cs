using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using ISubscription = EventDrivenThinking.EventInference.Subscriptions.ISubscription;

namespace EventDrivenThinking.Integrations.EventStore
{
    [DebuggerDisplay("EventStore Stream Subscriber, {EventType.Name}")]
    public class ProjectionEventStreamSubscriptionProvider<TEvent>
        : SingleEventStreamSubscriptionProvider,IEventSubscriptionProvider<IProjectionEventStream, IProjectionSchema, TEvent>
    where TEvent:IEvent
    {
        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;
        private readonly IProjectionEventStreamRepository _repo;
        private IProjectionEventStream _stream;
        private IProjectionSchema _schema;

        private static readonly string STREAM_NAME = $"$et-{typeof(TEvent).Name}";

        public ProjectionEventStreamSubscriptionProvider(IEventStoreFacade eventStore, 
            IEventConverter eventConverter, IProjectionEventStreamRepository repo)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _repo = repo;
            
        }

        public IProjectionEventStream Stream
        {
            get
            {
                if (_stream == null)
                    _stream = _repo.GetStream(_schema.Type);
                return _stream;
            }
        }

        public string Type => "EventStore";

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }

        public bool CanMerge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            return other is ProjectionMultiEventStreamSubscriptionProvider || other is SingleEventStreamSubscriptionProvider;
        }

        public ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> Merge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            if (other is ProjectionMultiEventStreamSubscriptionProvider multiProvider)
            {
                return other.Merge(this);
            }
            else return new ProjectionMultiEventStreamSubscriptionProvider(_eventStore,  _eventConverter, _repo, Stream, _schema)
                .Merge(this)
                .Merge(other);
        }

        public async Task<ISubscription> Subscribe( IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription();

            if (!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            // Projections stream type might not be the same as origin of the event.
            // For instance Stream might go directly to memory, where as some events
            // come from EventStore
            // In such a case, we could assume to subscribe from the beginning 
            // or we could assume that we want to subscribe from now.
            // For now we subscribe from beginning - however this is a problem. 
            // We don't know the nature of this subscription - if it is temporal or not.
            // If it had been temporal, we would subscribe from now
            // It it had not been temporal, we would subscribe from beginning

            var position = await Stream.LastPosition(); 
            StreamRevision sr = StreamRevision.Start;
            if (position is StreamPosition lastPosition)
            {
                sr = lastPosition.IsStart ? StreamRevision.Start : new StreamRevision(lastPosition.StreamRevision + 1);
            }
            
            await _eventStore.SubscribeToStreamAsync(STREAM_NAME, sr, async (s, r, c) =>
            {
                using (var scope = factory.Scope())
                {
                    var handler = scope.CreateHandler<TEvent>();

                    var (m, e) = _eventConverter.Convert<TEvent>(r);

                    await handler.Execute(m, e);
                }
            }, ss => s.MakeLive(), true);
            return s;
        }

        public override Type EventType => typeof(TEvent);
    }
}