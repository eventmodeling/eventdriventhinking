using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Users;
using DeleteResult = EventStore.Client.DeleteResult;
using Position = EventStore.Client.Position;
using ResolvedEvent = EventStore.Client.ResolvedEvent;
using StreamMetadata = EventStore.Client.StreamMetadata;
using StreamMetadataResult = EventStore.Client.StreamMetadataResult;
using WriteResult = EventStore.Client.WriteResult;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IEventStoreFacade : IDisposable
    {
        UserCredentials DefaultCredentials { get; }
        IHttpEventStoreProjectionManagerClient ProjectionsManager { get; }
        EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; }
        EventStoreUserManagerClient UsersManager { get; }


        Task<(Position, StreamRevision)> GetLastStreamPosition(string streamName);

        Task<WriteResult> AppendToStreamAsync(string streamName, StreamRevision expectedRevision, IEnumerable<global::EventStore.Client.EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<WriteResult> AppendToStreamAsync(string streamName, AnyStreamRevision expectedRevision, IEnumerable<global::EventStore.Client.EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        

        Task<DeleteResult> SoftDeleteAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<DeleteResult> SoftDeleteAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<WriteResult> SetStreamMetadataAsync(string streamName, AnyStreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<WriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        IAsyncEnumerable<ResolvedEvent> ReadAllAsync(Direction direction, Position position, ulong maxCount,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, FilterOptions filterOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        IAsyncEnumerable<ResolvedEvent> ReadStreamAsync(Direction direction, string streamName, StreamRevision revision, ulong count,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToAllAsync(Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false, Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            FilterOptions filterOptions = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToAllAsync(Position start, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, FilterOptions filterOptions = null, Func<IStreamSubscription, Position, CancellationToken, Task> checkpointReached = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, StreamRevision start,
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            Action<IStreamSubscription> onLiveProcessingStarted = null, 
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<DeleteResult> TombstoneAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<DeleteResult> TombstoneAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        
    }

    public interface IHttpEventStoreProjectionManagerClient
    {
        Task EnableAll();

        UserCredentials DefaultCredentials { get; }

        Task EnableAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task AbortAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task DisableAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task CreateOneTimeAsync(string query, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task CreateTransientAsync(string name, string query, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<JsonDocument> GetResultAsync(string name, string partition = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T> GetResultAsync<T>(string name, string partition = null, JsonSerializerOptions serializerOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<JsonDocument> GetStateAsync(string name, string partition = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T> GetStateAsync<T>(string name, string partition = null, JsonSerializerOptions serializerOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<ProjectionDetails> GetStatusAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        IAsyncEnumerable<ProjectionDetails> ListAllAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task UpdateAsync(string name, string query, bool? emitEnabled = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());
    }

    class HttpEventStoreProjectionManagerClient : IHttpEventStoreProjectionManagerClient
    {
        private EventStoreProjectionManagerClient _client;
        private EventStoreClient _parent;
        private readonly Func<UserCredentials> _defaultCredentials;

        public HttpEventStoreProjectionManagerClient(EventStoreClient parent, Func<UserCredentials> defaultCredentials)
        {
            _parent = parent;
            _defaultCredentials = defaultCredentials;
            _client = parent.ProjectionsManager;
        }

        public async Task EnableAll()
        {
            await foreach (var p in ListAllAsync())
            {
                await EnableAsync(p.Name);
            }

        }

        public UserCredentials DefaultCredentials => _defaultCredentials();
        public Task EnableAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.EnableAsync(name, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task AbortAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.AbortAsync(name, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task DisableAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.DisableAsync(name, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task CreateOneTimeAsync(string query, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.CreateOneTimeAsync(query, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.CreateContinuousAsync(name, query, trackEmittedStreams, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task CreateTransientAsync(string name, string query, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.CreateTransientAsync(name, query, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<JsonDocument> GetResultAsync(string name, string partition = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetResultAsync(name, partition, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<T> GetResultAsync<T>(string name, string partition = null, JsonSerializerOptions serializerOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetResultAsync<T>(name, partition, serializerOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<JsonDocument> GetStateAsync(string name, string partition = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetStateAsync(name, partition, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<T> GetStateAsync<T>(string name, string partition = null, JsonSerializerOptions serializerOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetStateAsync<T>(name, partition, serializerOptions, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.ListOneTimeAsync(DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.ListContinuousAsync(DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task<ProjectionDetails> GetStatusAsync(string name, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.GetStatusAsync(name, DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ProjectionDetails> ListAllAsync(UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.ListAllAsync(DefaultCredentials ?? userCredentials, cancellationToken);
        }

        public Task UpdateAsync(string name, string query, bool? emitEnabled = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _client.UpdateAsync(name, query, emitEnabled, DefaultCredentials ?? userCredentials, cancellationToken);
        }
    }
}
