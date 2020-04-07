using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;
using Google.Protobuf.Reflection;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class MultiEventSubscriptionProvider : ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        readonly List<SingleEventSubscriptionProvider> _providers;

        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;
        private IProjectionSchema _schema;

        public MultiEventSubscriptionProvider(SingleEventSubscriptionProvider singleEventSubscription, 
            IEventStoreFacade eventStore, IEventConverter eventConverter,
            IProjectionSchema schema)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _providers = new List<SingleEventSubscriptionProvider>(){ singleEventSubscription };
            _schema = schema;
        }

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }

        public virtual bool CanMerge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            return other is MultiEventSubscriptionProvider || other is SingleEventSubscriptionProvider;
        }

        public virtual ISubscriptionProvider<IProjection, IProjectionSchema> Merge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            if (other is MultiEventSubscriptionProvider mp)
            {
                _providers.AddRange(mp._providers);
            }
            else
            {
                _providers.Add(other as SingleEventSubscriptionProvider);
            }
            return this;
        }

        public virtual async Task<ISubscription> Subscribe( IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription();
            var supportedTypes = _providers.Select(x => x.EventType).ToHashSet();
            string projectionStreamName = null;
            if (args == null || args.Length == 0)
                projectionStreamName = $"{_schema.Category}Projection-{_schema.ProjectionHash}";
            else
                projectionStreamName = $"{_schema.Category}Projection-{args[0]}";

            await _eventStore.SubscribeToStreamAsync(projectionStreamName, StreamRevision.Start,  
                async (s, r, c) =>
            {
                var type = _schema.EventByName(r.Event.EventType);
                if (type != null && supportedTypes.Contains(type))
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler(type);

                        var (m, e) = _eventConverter.Convert(type, r);

                        await handler.Execute(m, e);
                    }
                }
            }, ss => s.MakeLive());

            return s;
        }
    }
}