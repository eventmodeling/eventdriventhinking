using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.Logging;
using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Users;
using EventStore.ClientAPI;
using ApiCredentials= EventStore.ClientAPI.SystemData.UserCredentials;
using DeleteResult = EventStore.Client.DeleteResult;
using Position = EventStore.Client.Position;
using ResolvedEvent = EventStore.Client.ResolvedEvent;
using StreamMetadata = EventStore.Client.StreamMetadata;
using StreamMetadataResult = EventStore.Client.StreamMetadataResult;
using WriteResult = EventStore.Client.WriteResult;

namespace EventDrivenThinking.EventInference.EventStore
{
    public class EventStoreFacade : IEventStoreFacade
    {
        
        private static readonly Serilog.ILogger Log = LoggerFactory.For<EventStoreFacade>();
        private readonly HttpEventStoreChannel _httpClient;
        private readonly TcpEventStoreChannel _tcpClient;
        private readonly IEventStoreChannel _client;
        
        public UserCredentials DefaultCredentials { get; private set; }

        public EventStoreFacade(string httpUrl, string tcpUrl, string user, string password)
        {
            var tcpUri = new Uri(tcpUrl);
            
            ConnectionSettings tcpSettings = ConnectionSettings.Create()
                .UseSslConnection(false)
                .KeepRetrying()
                .SetDefaultUserCredentials(new ApiCredentials(user, password))
                .Build();

            var tcp = EventStoreConnection.Create(tcpSettings, tcpUri);
            tcp.ConnectAsync().GetAwaiter().GetResult();
            Log.Debug("TCP: Connected.");


            var httpSettings = new EventStoreClientSettings();
            httpSettings.ConnectivitySettings.Address = new Uri(httpUrl);
            DefaultCredentials = new UserCredentials(user, password);
            _httpClient = new HttpEventStoreChannel(new EventStoreClient(httpSettings));
            _tcpClient = new TcpEventStoreChannel(tcp);

            _client = _tcpClient;
        }
        

        public Task<WriteResult> AppendToStreamAsync(string streamName, StreamRevision expectedRevision, IEnumerable<global::EventStore.Client.EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.AppendToStreamAsync(streamName, expectedRevision, eventData, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<WriteResult> AppendToStreamAsync(string streamName, AnyStreamRevision expectedRevision, IEnumerable<global::EventStore.Client.EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.AppendToStreamAsync(streamName, expectedRevision, eventData, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public void Dispose()
        {
            Log.Debug("Disposing EventStore connections....");
            _tcpClient.Dispose();
            _httpClient.Dispose();
        }

        public Task<DeleteResult> SoftDeleteAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SoftDeleteAsync(streamName, expectedRevision, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<DeleteResult> SoftDeleteAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SoftDeleteAsync(streamName, expectedRevision, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetStreamMetadataAsync(streamName, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, AnyStreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SetStreamMetadataAsync(streamName, expectedRevision, metadata, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SetStreamMetadataAsync(streamName, expectedRevision, metadata, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ResolvedEvent> ReadAllAsync(Direction direction, Position position, ulong maxCount,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, FilterOptions filterOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.ReadAllAsync(direction, position, maxCount, configureOperationOptions, resolveLinkTos, filterOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public async Task<Position> GetLastStreamPosition(string streamName)
        {
            try
            {
                var m = await _httpClient.GetStreamMetadataAsync(streamName);
                if (m.MetastreamRevision.HasValue)
                {
                    await foreach (var i in _httpClient.ReadStreamAsync(Direction.Backwards,
                        streamName,
                        StreamRevision.End, 1, null, true))
                    {
                        return i.Event.Position;
                    }
                }
            }
            catch (StreamNotFoundException ex)
            {
            }
            return Position.Start;
        }

        public IAsyncEnumerable<ResolvedEvent> ReadStreamAsync(Direction direction, string streamName, StreamRevision revision, ulong count,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //return _httpClient.ReadStreamAsync(direction, streamName, revision, count, configureOperationOptions, resolveLinkTos, DefaultCredentials ?? userCredentials, cancellationToken);
            return _client.ReadStreamAsync(direction, streamName, revision, count, configureOperationOptions, resolveLinkTos, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<IStreamSubscription> SubscribeToAllAsync(Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false, Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            FilterOptions filterOptions = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SubscribeToAllAsync(eventAppeared, resolveLinkTos, subscriptionDropped, filterOptions, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<IStreamSubscription> SubscribeToAllAsync(Position start, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, FilterOptions filterOptions = null, Func<IStreamSubscription, Position, CancellationToken, Task> checkpointReached = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.SubscribeToAllAsync(start, eventAppeared, resolveLinkTos, subscriptionDropped, filterOptions, checkpointReached, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, 
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, 
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, 
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, 
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Log.Debug("SubscribeToStreamAsync {streamName}", streamName);

            return _client.SubscribeToStreamAsync(streamName, eventAppeared, resolveLinkTos, subscriptionDropped, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public async Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, 
            StreamRevision start,
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            Action<IStreamSubscription> onLiveProcessingStarted, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Log.Debug("SubscribeToStreamAsync {streamName}", streamName);
            if (onLiveProcessingStarted != null && _client == _tcpClient)
            {
                return await _tcpClient.SubscribeToStreamAsync(streamName, start, eventAppeared, onLiveProcessingStarted, resolveLinkTos, subscriptionDropped, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
            }
            else
            {
                var result = await _client.SubscribeToStreamAsync(streamName, start, eventAppeared, resolveLinkTos, subscriptionDropped, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
                onLiveProcessingStarted?.Invoke(result);
                return result;
            }
        }

        public Task<DeleteResult> TombstoneAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.TombstoneAsync(streamName, expectedRevision, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<DeleteResult> TombstoneAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.TombstoneAsync(streamName, expectedRevision, configureOperationOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        private IHttpEventStoreProjectionManagerClient _httpEventStoreProjectionManagerClient;
        public IHttpEventStoreProjectionManagerClient ProjectionsManager
        {
            get
            {
                if(_httpEventStoreProjectionManagerClient == null)
                    _httpEventStoreProjectionManagerClient = new HttpEventStoreProjectionManagerClient(this._httpClient.Client, () => this.DefaultCredentials);
                return _httpEventStoreProjectionManagerClient;
            }
        }

        public EventStorePersistentSubscriptionsClient PersistentSubscriptions => _client.PersistentSubscriptions;

        public EventStoreUserManagerClient UsersManager => _client.UsersManager;
    }
}