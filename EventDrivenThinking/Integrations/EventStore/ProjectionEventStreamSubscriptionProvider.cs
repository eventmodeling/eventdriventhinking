using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ProjectionMultiEventStreamSubscriptionProvider : ISubscriptionProvider<IProjectionEventStream, IProjectionSchema>
    {
        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;
        private readonly IProjectionEventStreamRepository _repo;
        private readonly HashSet<Type> _events;
        private readonly IProjectionEventStream _stream;

        private readonly IProjectionSchema _schema;

        public ProjectionMultiEventStreamSubscriptionProvider(IEventStoreFacade eventStore, 
            IEventConverter eventConverter, 
            IProjectionEventStreamRepository repo,
            IProjectionEventStream stream, 
            IProjectionSchema schema)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _repo = repo;
            _stream = stream;
            _schema = schema;
            _events = new HashSet<Type>();
        }

        public void Init(IProjectionSchema schema)
        {
            throw new NotSupportedException();
        }

        public bool CanMerge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            return other is ProjectionMultiEventStreamSubscriptionProvider || other is SingleEventStreamSubscriptionProvider;
        }

        public ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> Merge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            if (other is ProjectionMultiEventStreamSubscriptionProvider mp)
            {
                foreach (var i in mp._events)
                    _events.Add(i);
            }
            else
            {
                _events.Add(((SingleEventStreamSubscriptionProvider)other).EventType);
            }
            return this;
        }

        public async Task<ISubscription> Subscribe( IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription(true);

            var lastPosition = (StreamPosition)await _stream.LastPosition();
            
            string[] prefixes = _events.Select(x => x.Name).ToArray();
            FilterOptions filters = new FilterOptions(EventTypeFilter.Prefix(prefixes));

            // be very careful. We need to subscribe after the global position.

            await _eventStore.SubscribeToAllAsync(lastPosition.GlobalPosition, 
                async (s, r, c) =>
                {
                    var type = _schema.EventByName(r.Event.EventType);
                    if (type != null && _events.Contains(type))
                    {
                        using (var scope = factory.Scope())
                        {
                            var handler = scope.CreateHandler(type);

                            var (m, e) = _eventConverter.Convert(type, r);

                            await handler.Execute(m, e);
                        }
                    }
                },true,null,filters);

            return s;
        }
    }

    public abstract class SingleEventStreamSubscriptionProvider
    {
        
        public abstract Type EventType { get; }
    }
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

            var lastPosition = (StreamPosition)await Stream.LastPosition();
            StreamRevision sr = lastPosition.IsStart ? StreamRevision.Start : new StreamRevision(lastPosition.StreamRevision+1);

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