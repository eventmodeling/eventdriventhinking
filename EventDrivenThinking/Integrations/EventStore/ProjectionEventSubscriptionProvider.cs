using System;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventStore.Client;

namespace EventDrivenThinking.Integrations.EventStore
{

    /// <summary>
    /// Owner is what, IProjection or IProcessor?
    /// The source of subscription per type in app can be only one.
    /// </summary>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    public class ProjectionEventSubscriptionProvider<TEvent> : SingleEventSubscriptionProvider,
        IEventSubscriptionProvider<IProjection, TEvent>
        where TEvent : IEvent
    {
        private readonly IEventStoreFacade _eventStore;

        public ProjectionEventSubscriptionProvider(IEventStoreFacade eventStore)
        {
            _eventStore = eventStore;
        }

        public override Type EventType => typeof(TEvent);

        public override async Task Subscribe(ISchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            IProjectionSchema pSchema = (IProjectionSchema) schema;

            if(factory.SupportedEventTypes.OfType<TEvent>().Count() != 1)
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            string projectionStreamName = null;
            if (args == null || args.Length == 0)
                projectionStreamName = $"{pSchema.Category}Projection-{pSchema.ProjectionHash}";
            else
                projectionStreamName = $"{pSchema.Category}Projection-{args[0]}";

            await _eventStore.SubscribeToStreamAsync(projectionStreamName, async (s, r, c) =>
            {
                
            });
        }
    }
}