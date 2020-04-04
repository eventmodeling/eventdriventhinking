using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using Google.Protobuf.Reflection;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class MultiEventSubscriptionProvider : ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        readonly List<SingleEventSubscriptionProvider> _providers;

        private readonly IEventStoreFacade _eventStore;
        private readonly IEventConverter _eventConverter;

        public MultiEventSubscriptionProvider(SingleEventSubscriptionProvider singleEventSubscription, IEventStoreFacade eventStore, IEventConverter eventConverter)
        {
            _eventStore = eventStore;
            _eventConverter = eventConverter;
            _providers = new List<SingleEventSubscriptionProvider>(){ singleEventSubscription };
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

        public virtual async Task Subscribe(IProjectionSchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            var supportedTypes = _providers.Select(x => x.EventType).ToHashSet();
            string projectionStreamName = null;
            if (args == null || args.Length == 0)
                projectionStreamName = $"{schema.Category}Projection-{schema.ProjectionHash}";
            else
                projectionStreamName = $"{schema.Category}Projection-{args[0]}";

            await _eventStore.SubscribeToStreamAsync(projectionStreamName, async (s, r, c) =>
            {
                var type = schema.EventByName(r.Event.EventType);
                if (type != null && supportedTypes.Contains(type))
                {
                    var handler = factory.CreateHandler(type);

                    var (m, e) = _eventConverter.Convert(type, r);

                    await handler.Execute(m, e);
                }
            });
        }
    }
}