using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Logging;
using EventStore.Client;
using Newtonsoft.Json;
using Serilog;

namespace EventDrivenThinking.Integrations.EventStore
{
    //public class EventStoreModelProjectionSubscriber<TModel> : IModelProjectionSubscriber<TModel>
    //{
    //    private static ILogger Log = LoggerFactory.For<EventStoreModelProjectionSubscriber<TModel>>();
    //    class Subscription : ISubscription
    //    {
    //        public string StreamName { get; }
    //        private IStreamSubscription _eventStoreSubscription;

    //        public Subscription(string streamName)
    //        {
    //            StreamName = streamName;
    //        }


    //        public void Initialized(IStreamSubscription s)
    //        {
    //            _eventStoreSubscription = s;
    //        }
    //    }
    //    private readonly IEventStoreFacade _connection;
    //    private readonly IProjectionSchema _schema;
    //    private readonly IEventConverter _eventConverter;

    //    public EventStoreModelProjectionSubscriber(IEventStoreFacade connection, IProjectionSchemaRegister projectionSchemaRegister, IEventConverter eventConverter)
    //    {
    //        _connection = connection;
    //        _eventConverter = eventConverter;
    //        _schema = projectionSchemaRegister.FindByModelType(typeof(TModel));
    //    }


    //    public async Task<ISubscription> SubscribeToStream(
    //        Func<(EventMetadata, IEvent)[], Task> onEvents,
    //        Action<ISubscription> onLiveStarted = null,
    //        Guid? partitionId = null,
    //        long? location = null)
    //    {
    //        string streamName = ServiceConventions.GetProjectionStreamFromType(_schema.Type);
    //        if (partitionId.HasValue)
    //            streamName += $"Partition-{partitionId}";
    //        else 
    //            streamName += $"-{_schema.ProjectionHash}";

    //        Subscription s = new Subscription(streamName);
            
            
    //        var subscription = await _connection.SubscribeToStreamAsync(streamName,  
    //            StreamRevision.Start, 
    //            async (subscription, e,ea) =>
    //            {
    //                Log.Debug("Received event {EventType}, IsLink: {isLink}", e.Event.EventType, e.Link != null);
    //                var eventType = _schema.EventByName(e.Event.EventType);
    //                var (em, eventInstance) = _eventConverter.Convert(eventType, e);
    //                await onEvents(new (EventMetadata, IEvent)[] {(em, eventInstance)});
    //            },sc => onLiveStarted?.Invoke(s),
    //            true);


    //        s.Initialized(subscription);

    //        return s;
    //    }
    //}

   
}
