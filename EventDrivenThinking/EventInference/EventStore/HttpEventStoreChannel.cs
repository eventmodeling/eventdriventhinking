using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Users;
using DeleteResult = EventStore.Client.DeleteResult;
using EventData = EventStore.Client.EventData;
using Position = EventStore.Client.Position;
using ResolvedEvent = EventStore.Client.ResolvedEvent;
using StreamMetadata = EventStore.Client.StreamMetadata;
using StreamMetadataResult = EventStore.Client.StreamMetadataResult;
using WriteResult = EventStore.Client.WriteResult;

namespace EventDrivenThinking.EventInference.EventStore
{
    internal interface IEventStoreChannel
    {
        Task<WriteResult> AppendToStreamAsync(string streamName, StreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<WriteResult> AppendToStreamAsync(string streamName, AnyStreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        void Dispose();

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

        Task<IStreamSubscription> SubscribeToAllAsync(Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            FilterOptions filterOptions = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, 
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToAllAsync(Position start, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, FilterOptions filterOptions = null, 
            Func<IStreamSubscription, Position, CancellationToken, Task> checkpointReached = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, StreamRevision start,
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, 
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<DeleteResult> TombstoneAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        Task<DeleteResult> TombstoneAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken());

        EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; }
        EventStoreProjectionManagerClient ProjectionsManager { get; }
        EventStoreUserManagerClient UsersManager { get; }
    }

    class HttpStreamSubscription : IStreamSubscription
    {
        private StreamSubscription _inner;

        public HttpStreamSubscription()
        {
            
        }
        public IStreamSubscription WriteThough(StreamSubscription s)
        {
            _inner = s;
            return this;
        }
        public void Dispose()
        {
            _inner?.Dispose();
        }
    }
    class HttpEventStoreChannel : IEventStoreChannel
    {
        public readonly EventStoreClient Client;

        public HttpEventStoreChannel(EventStoreClient client)
        {
            Client = client;
        }

        public Task<WriteResult> AppendToStreamAsync(string streamName, StreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.AppendToStreamAsync(streamName, expectedRevision, eventData, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<WriteResult> AppendToStreamAsync(string streamName, AnyStreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.AppendToStreamAsync(streamName, expectedRevision, eventData, configureOperationOptions, userCredentials, cancellationToken);
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public Task<DeleteResult> SoftDeleteAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.SoftDeleteAsync(streamName, expectedRevision, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<DeleteResult> SoftDeleteAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.SoftDeleteAsync(streamName, expectedRevision, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.GetStreamMetadataAsync(streamName, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, AnyStreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.SetStreamMetadataAsync(streamName, expectedRevision, metadata, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.SetStreamMetadataAsync(streamName, expectedRevision, metadata, configureOperationOptions, userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ResolvedEvent> ReadAllAsync(Direction direction, Position position, ulong maxCount,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, FilterOptions filterOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.ReadAllAsync(direction, position, maxCount, configureOperationOptions, resolveLinkTos, filterOptions, userCredentials, cancellationToken);
        }

        public IAsyncEnumerable<ResolvedEvent> ReadStreamAsync(Direction direction, string streamName, StreamRevision revision, ulong count,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.ReadStreamAsync(direction, streamName, revision, count, configureOperationOptions, resolveLinkTos, userCredentials, cancellationToken);
        }

        public async Task<IStreamSubscription> SubscribeToAllAsync(Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, 
            bool resolveLinkTos = false, Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            FilterOptions filterOptions = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HttpStreamSubscription subscription = new HttpStreamSubscription();
            return subscription.WriteThough(await Client.SubscribeToAllAsync(async (s, r, c) => eventAppeared?.Invoke(subscription.WriteThough(s), r, c), 
                resolveLinkTos,
                (s, r, e) => subscriptionDropped?.Invoke(subscription.WriteThough(s), r, e),
                filterOptions, configureOperationOptions, userCredentials, cancellationToken));
        }

        public async Task<IStreamSubscription> SubscribeToAllAsync(Position start, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, FilterOptions filterOptions = null,
            Func<IStreamSubscription, Position, CancellationToken, Task> checkpointReached = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HttpStreamSubscription subscription = new HttpStreamSubscription();
            return subscription.WriteThough(await Client.SubscribeToAllAsync(start, 
                async (s,r,c) => eventAppeared?.Invoke(subscription.WriteThough(s),r,c), 
                resolveLinkTos,
                (s,r,e) => subscriptionDropped?.Invoke(subscription.WriteThough(s),r,e), 
                filterOptions, 
                async (s,p,c) => checkpointReached?.Invoke(subscription.WriteThough(s),p,c), 
                configureOperationOptions, userCredentials, cancellationToken));
        }

        public async Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HttpStreamSubscription subscription = new HttpStreamSubscription();
            return subscription.WriteThough(await Client.SubscribeToStreamAsync(streamName,
                async (s, r, c) => eventAppeared?.Invoke(subscription.WriteThough(s), r, c),
                resolveLinkTos,
                (s, r, e) => subscriptionDropped?.Invoke(subscription.WriteThough(s), r, e),
                configureOperationOptions, userCredentials, cancellationToken));
        }

        public async Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, StreamRevision start, 
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HttpStreamSubscription subscription = new HttpStreamSubscription();
            return subscription.WriteThough(await Client.SubscribeToStreamAsync(streamName, start,
                async (s, r, c) => eventAppeared?.Invoke(subscription.WriteThough(s), r, c),
                resolveLinkTos,
                (s, r, e) => subscriptionDropped?.Invoke(subscription.WriteThough(s), r, e),
                configureOperationOptions, userCredentials, cancellationToken));
        }

        public Task<DeleteResult> TombstoneAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.TombstoneAsync(streamName, expectedRevision, configureOperationOptions, userCredentials, cancellationToken);
        }

        public Task<DeleteResult> TombstoneAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return Client.TombstoneAsync(streamName, expectedRevision, configureOperationOptions, userCredentials, cancellationToken);
        }

        public EventStorePersistentSubscriptionsClient PersistentSubscriptions => Client.PersistentSubscriptions;

        public EventStoreProjectionManagerClient ProjectionsManager => Client.ProjectionsManager;

        public EventStoreUserManagerClient UsersManager => Client.UsersManager;
    }
}