using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventDrivenThinking.Integrations.EventStore
{
    public class EventStoreModelProjectionSubscriber<TModel> : IModelProjectionSubscriber<TModel>
    {
        class Subscription : ISubscription
        {
            public string StreamName { get; }
            private EventStoreCatchUpSubscription _eventStoreSubscription;

            public Subscription(string streamName)
            {
                StreamName = streamName;
            }

            public bool IsLive { get; set; }

            public void Initialized(EventStoreCatchUpSubscription s)
            {
                _eventStoreSubscription = s;
            }
        }
        private IEventStoreConnection _connection;
        private IProjectionSchema _schema;

        public EventStoreModelProjectionSubscriber(IEventStoreConnection connection, IProjectionSchemaRegister projectionSchemaRegister)
        {
            _connection = connection;
            _schema = projectionSchemaRegister.FindByModelType(typeof(TModel));
        }


        public async Task<ISubscription> SubscribeToStream(
            Func<(EventMetadata, IEvent)[], Task> onEvents, 
            Action<ISubscription> liveProcessingStarted,
            Guid? partitionId = null,
            long? location = null)
        {
            string streamName = ServiceConventions.GetProjectionStreamFromType(_schema.Type);
            if (partitionId.HasValue)
                streamName += $"Partition-{partitionId}";

            Subscription s = new Subscription(streamName);
            //Dictionary<string, Type> _eventTypeDict = expectedEventTypes.ToDictionary(x => x.Name);
            var subscription = _connection.SubscribeToStreamFrom(streamName, location, CatchUpSubscriptionSettings.Default,
                async (subscription, e) =>
                {
                    var eventString = Encoding.UTF8.GetString(e.Event.Data);
                    var em = JsonConvert.DeserializeObject<EventMetadata>(Encoding.UTF8.GetString(e.Event.Metadata));
                    var eventType = _schema.EventByName(e.Event.EventType);
                    var eventInstance = (IEvent)JsonConvert.DeserializeObject(eventString, eventType);
                    await onEvents(new (EventMetadata, IEvent)[] {(em, eventInstance)});
                },
                subscription =>
                {
                    s.IsLive = true;
                    liveProcessingStarted(s);
                });
            s.Initialized(subscription);

            return s;
        }
    }
}
