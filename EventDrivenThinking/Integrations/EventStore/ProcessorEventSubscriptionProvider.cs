using System;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class ProcessorEventSubscriptionProvider<TEvent> : 
        IEventSubscriptionProvider<IProcessor, IProcessorSchema, TEvent>
        where TEvent : IEvent
    {
        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;
        private readonly IServiceProvider _serviceProvider;
        private IProcessorSchema _schema;
        
        public ProcessorEventSubscriptionProvider(IEventStoreFacade eventStore, 
            IEventConverter eventConverter, IServiceProvider serviceProvider)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _serviceProvider = serviceProvider;
        }


        public string Type => "EventStore";

        public void Init(IProcessorSchema schema)
        {
            if (_schema == null || Equals(_schema, schema))
                _schema = schema;
            else throw new InvalidOperationException();
        }

        public bool CanMerge(ISubscriptionProvider<IProcessor, IProcessorSchema> other)
        {
            return false;
        }

        public ISubscriptionProvider<IProcessor, IProcessorSchema> Merge(ISubscriptionProvider<IProcessor, IProcessorSchema> other)
        {
            throw new NotImplementedException();
        }

        public async Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null)
        {
            if (!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException(
                    $"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            // uhhh and we need to know from when...
            Type checkpointRepo = typeof(ICheckpointRepository<,>).MakeGenericType(_schema.Type, typeof(TEvent));

            var repo = (ICheckpointEventRepository<TEvent>) _serviceProvider.GetService(checkpointRepo);

            string streamName = $"$et-{typeof(TEvent).Name}";

            var lastCheckpoint = await repo.GetLastCheckpoint();

            StreamRevision start = StreamRevision.Start;

            if (!lastCheckpoint.HasValue)
            {
                // this is the first time we run this processor. 
                var (globalPosition, streamRevision )= await _eventStore.GetLastStreamPosition(streamName);
                start = streamRevision;
                await repo.SaveCheckpoint(start.ToUInt64());
            }
            else
            {
                start = new StreamRevision(lastCheckpoint.Value+1);
            }
            Subscription s = new Subscription();
            await _eventStore.SubscribeToStreamAsync(streamName, start,
                async (s, r, c) =>
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler<TEvent>();

                        var (m, e) = _eventConverter.Convert<TEvent>(r);

                        await handler.Execute(m, e);

                        await repo.SaveCheckpoint(r.Link.EventNumber.ToUInt64());
                    }

                },ss => s.MakeLive(), true);

            return s;
        }
    }
}