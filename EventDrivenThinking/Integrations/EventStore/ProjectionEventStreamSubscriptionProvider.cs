using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.Integrations.EventStore
{
    
    public class ProjectionEventStreamSubscriptionProvider<TEvent>
        : IEventSubscriptionProvider<IProjectionEventStream, IProjectionSchema, TEvent>
    where TEvent:IEvent
    {
        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;
        private readonly IProjectionEventStreamRepository _repo;
        private IProjectionEventStream _stream;
        
        private static readonly string STREAM_NAME = $"et-{typeof(TEvent).Name}";

        public ProjectionEventStreamSubscriptionProvider(IEventStoreFacade eventStore, 
            IEventConverter eventConverter, IProjectionEventStreamRepository repo)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _repo = repo;
        }

        public bool CanMerge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            return false;
        }

        public ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> Merge(ISubscriptionProvider<IProjectionEventStream, IProjectionSchema> other)
        {
            throw new NotSupportedException();
        }

        public async Task Subscribe(IProjectionSchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            if (!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            if (_stream == null)
                _stream = _repo.GetStream(schema.Type);

            var lastPosition = (StreamPosition)await _stream.LastPosition();
            StreamRevision sr = new StreamRevision(lastPosition.EventStorePosition.CommitPosition);
            await _eventStore.SubscribeToStreamAsync(STREAM_NAME, sr, async (s, r, c) =>
            {
                var handler = factory.CreateHandler<TEvent>();

                var (m, e) = _eventConverter.Convert<TEvent>(r);

                await handler.Execute(m, e);
            });
        }
    }
}