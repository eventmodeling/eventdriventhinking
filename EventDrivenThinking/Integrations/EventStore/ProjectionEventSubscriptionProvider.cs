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
        IEventSubscriptionProvider<IProjection, IProjectionSchema, TEvent>
        where TEvent : IEvent
    {
       
        public ProjectionEventSubscriptionProvider(IEventStoreFacade eventStore, IEventConverter eventConverter) : base(eventStore, eventConverter) { }

        public override Type EventType => typeof(TEvent);

        public override async Task Subscribe(IProjectionSchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            if(!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");

            string projectionStreamName = null;
            if (args == null || args.Length == 0)
                projectionStreamName = $"{schema.Category}Projection-{schema.ProjectionHash}";
            else
                projectionStreamName = $"{schema.Category}Projection-{args[0]}";

            await _eventStore.SubscribeToStreamAsync(projectionStreamName, async (s, r, c) =>
            {
                var type = schema.EventByName(r.Event.EventType);
                if (type == typeof(TEvent))
                {
                    var handler = factory.CreateHandler<TEvent>();

                    var (m, e) = _eventConverter.Convert<TEvent>(r);

                    await handler.Execute(m, e);
                }
            });
        }
    }
}