using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;
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

        public string Type => "EventStore";

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
}