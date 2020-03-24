using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.Utils;
using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Users;
using EventStore.ClientAPI;
using DeleteResult = EventStore.Client.DeleteResult;
using EventData = EventStore.Client.EventData;
using EventData2 = EventStore.ClientAPI.EventData;
using Position = EventStore.Client.Position;
using EventRecord = EventStore.Client.EventRecord;
using RecordedEvent= EventStore.ClientAPI.RecordedEvent;
using Position2 = EventStore.ClientAPI.Position;
using ResolvedEvent = EventStore.Client.ResolvedEvent;
using StreamMetadata = EventStore.Client.StreamMetadata;
using StreamMetadataResult = EventStore.Client.StreamMetadataResult;
using WriteResult = EventStore.Client.WriteResult;
using UserCredentials2=EventStore.ClientAPI.SystemData.UserCredentials;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface IStreamSubscription : IDisposable
    {

    }

    class TcpSubscription : IStreamSubscription
    {
        private readonly Action _disposeAction;

        public TcpSubscription(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction();
        }
    }
    static class EventDataConverter
    {
        public static DateTime FromTicksSinceEpoch(this long value)
        {
            return new DateTime(DateTime.UnixEpoch.Ticks + value, DateTimeKind.Utc);
        }

        public static long ToTicksSinceEpoch(this DateTime value)
        {
            return (value - DateTime.UnixEpoch).Ticks;
        }
        public static EventData2 Convert(this EventData d)
        {
            return new EventData2(d.EventId.ToGuid(), d.Type, d.ContentType == "application/json", d.Data, d.Metadata);
        }

        public static long Convert(this StreamRevision s)
        {
            return (long)s.ToUInt64();
        }
        public static EventRecord Convert(this RecordedEvent e, Position2? position)
        {
            if (e == null)
                return null;

            IDictionary<string, string> meta = new Dictionary<string, string>()
            {
                { "type",e.EventType},
                { "created", e.Created.ToTicksSinceEpoch().ToString()},
                { "content-type", e.IsJson ? "application/json" : "application/octet-stream"}
            };
            Position originalPosition = new Position();
            if (position.HasValue)
            {
                originalPosition = new Position((ulong)position.Value.CommitPosition, (ulong)position.Value.PreparePosition);
            }
            return new EventRecord(e.EventStreamId,Uuid.FromGuid(e.EventId), new StreamRevision((ulong)e.EventNumber),
                originalPosition, meta,e.Data, e.Metadata);
        }
        
        public static Position? Convert(this Position2? src)
        {
            if(src.HasValue)
                return new Position((ulong)src.Value.CommitPosition, (ulong)src.Value.PreparePosition);
            return null;
        }
        public static long? ConvertCommit(this Position2? src)
        {
            return src?.CommitPosition;
        }

        public static SubscriptionDroppedReason Convert(this SubscriptionDropReason r)
        {
            switch (r)
            {
                case SubscriptionDropReason.MaxSubscribersReached:
                case SubscriptionDropReason.ProcessingQueueOverflow:
                case SubscriptionDropReason.CatchUpError:
                case SubscriptionDropReason.PersistentSubscriptionDeleted:
                case SubscriptionDropReason.AccessDenied:
                case SubscriptionDropReason.NotAuthenticated:
                case SubscriptionDropReason.NotFound:
                case SubscriptionDropReason.Unknown:
                case SubscriptionDropReason.ServerError:
                    return SubscriptionDroppedReason.ServerError;

                case SubscriptionDropReason.EventHandlerException:
                case SubscriptionDropReason.UserInitiated:
                case SubscriptionDropReason.SubscribingError:
                    return SubscriptionDroppedReason.SubscriberError;

                case SubscriptionDropReason.ConnectionClosed:
                    return SubscriptionDroppedReason.Disposed;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(r), r, null);
            }
        }
        public static Position Convert(this Position2 src)
        {
            return new Position((ulong)src.CommitPosition, (ulong)src.PreparePosition);
        }
        public static Position2 Convert(this Position src)
        {
            return new Position2((int)src.CommitPosition, (int)src.PreparePosition);
        }
        public static int Convert(this AnyStreamRevision r)
        {
            if (r == AnyStreamRevision.NoStream)
                return ExpectedVersion.NoStream;
            else if (r == AnyStreamRevision.Any)
                return ExpectedVersion.Any;
            else if (r == AnyStreamRevision.StreamExists)
                return ExpectedVersion.StreamExists;
            else throw new NotImplementedException();
        }
        public static UserCredentials2 Convert(this UserCredentials c)
        {
            return new UserCredentials2(c.Username, c.Password);
        }
    }
    class TcpEventStoreChannel : IEventStoreChannel
    {
        private readonly IEventStoreConnection _client;

        public TcpEventStoreChannel(IEventStoreConnection tcp)
        {
            _client = tcp;
        }

        public async Task<WriteResult> AppendToStreamAsync(string streamName, StreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await _client.AppendToStreamAsync(streamName, (long) expectedRevision.ToUInt64(),
                eventData.Select(EventDataConverter.Convert));
            return new WriteResult(result.NextExpectedVersion, result.LogPosition.Convert());
        }

        public async Task<WriteResult> AppendToStreamAsync(string streamName, AnyStreamRevision expectedRevision, IEnumerable<EventData> eventData,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await _client.AppendToStreamAsync(streamName, expectedRevision.Convert(),
                eventData.Select(EventDataConverter.Convert));
            return new WriteResult(result.NextExpectedVersion, result.LogPosition.Convert());
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<DeleteResult> SoftDeleteAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await _client.DeleteStreamAsync(streamName, (long) expectedRevision.ToUInt64());
            return new DeleteResult(result.LogPosition.Convert());
        }

        public Task<DeleteResult> SoftDeleteAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<StreamMetadataResult> GetStreamMetadataAsync(string streamName, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, AnyStreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult> SetStreamMetadataAsync(string streamName, StreamRevision expectedRevision, StreamMetadata metadata,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<ResolvedEvent> ReadAllAsync(Direction direction, Position position, ulong maxCount,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false, FilterOptions filterOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            if (direction == Direction.Forwards)
            {
                var slices = await _client.ReadAllEventsForwardAsync(position.Convert(), (int)maxCount, resolveLinkTos);

                foreach (var i in slices.Events)
                {
                    yield return new ResolvedEvent(i.Event.Convert(i.OriginalPosition), i.Link.Convert(i.OriginalPosition), i.OriginalPosition.ConvertCommit());
                }
            }
            else
            {
                var slices = await _client.ReadAllEventsBackwardAsync(position.Convert(), (int)maxCount, resolveLinkTos);

                foreach (var i in slices.Events)
                {
                    yield return new ResolvedEvent(i.Event.Convert(i.OriginalPosition), i.Link.Convert(i.OriginalPosition), i.OriginalPosition.ConvertCommit());
                }
            }
        }

        public async IAsyncEnumerable<ResolvedEvent> ReadStreamAsync(Direction direction, string streamName,
            StreamRevision revision, ulong count,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, bool resolveLinkTos = false,
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (direction == Direction.Forwards)
            {
                var items = await _client.ReadStreamEventsForwardAsync(streamName, revision.Convert(), (int) count, resolveLinkTos);
                foreach(var i in items.Events)
                    yield return new ResolvedEvent(i.Event.Convert(i.OriginalPosition), i.Link.Convert(i.OriginalPosition), i.OriginalPosition.ConvertCommit());
            }
            else
            {
                var items = await _client.ReadStreamEventsBackwardAsync(streamName, revision.Convert(), (int)count, resolveLinkTos);
                foreach (var i in items.Events)
                    yield return new ResolvedEvent(i.Event.Convert(i.OriginalPosition), i.Link.Convert(i.OriginalPosition), i.OriginalPosition.ConvertCommit());
            }
        }

        public async Task<IStreamSubscription> SubscribeToAllAsync(Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            FilterOptions filterOptions = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if(filterOptions != null)
                throw new NotSupportedException();

            TcpSubscription subscription = null;
            var result = await _client.SubscribeToAllAsync(resolveLinkTos,
                async (s, r) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Dispose);

                    await eventAppeared?.Invoke(subscription,
                        new ResolvedEvent(r.Event.Convert(r.OriginalPosition), r.Link.Convert(r.OriginalPosition), r.OriginalPosition.ConvertCommit()),
                        CancellationToken.None);
                },
                async (s, d, e) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Dispose);
                    subscriptionDropped?.Invoke(subscription, d.Convert(), e);
                });

            if (subscription == null)
                subscription = new TcpSubscription(result.Dispose);

            return subscription;
        }

        public async Task<IStreamSubscription> SubscribeToAllAsync(Position start, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, FilterOptions filterOptions = null, 
            Func<IStreamSubscription, Position, CancellationToken, Task> checkpointReached = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (filterOptions != null)
                throw new NotSupportedException();

            TcpSubscription subscription = null;
            var result = _client.SubscribeToAllFrom(start.Convert(), CatchUpSubscriptionSettings.Default,  
                async (s, r) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Stop);

                    await eventAppeared?.Invoke(subscription,
                        new ResolvedEvent(r.Event.Convert(r.OriginalPosition), r.Link.Convert(r.OriginalPosition), r.OriginalPosition.ConvertCommit()),
                        CancellationToken.None);
                },
                (subscription) => { },
                async (s, d, e) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Stop);
                    subscriptionDropped?.Invoke(subscription, d.Convert(), e);
                });

            if (subscription == null)
                subscription = new TcpSubscription(result.Stop);

            return subscription;
        }

        public async Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared, 
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null, Action<EventStoreClientOperationOptions> configureOperationOptions = null, UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            TcpSubscription subscription = null;
            var result = await _client.SubscribeToStreamAsync(streamName, resolveLinkTos,
                async (s, r) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Dispose);

                    await eventAppeared?.Invoke(subscription,
                        new ResolvedEvent(r.Event.Convert(r.OriginalPosition), r.Link.Convert(r.OriginalPosition), r.OriginalPosition.ConvertCommit()),
                        CancellationToken.None);
                },
                async (s, d, e) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Dispose);
                    subscriptionDropped?.Invoke(subscription, d.Convert(), e);
                });
            if(subscription == null)
                subscription = new TcpSubscription(result.Dispose);
            return subscription;
        }

        public Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, StreamRevision start,
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return SubscribeToStreamAsync(streamName, start, eventAppeared, null, resolveLinkTos, subscriptionDropped,
                configureOperationOptions, userCredentials, cancellationToken);
        }

        public async Task<IStreamSubscription> SubscribeToStreamAsync(string streamName, StreamRevision start,
            Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
            Action<IStreamSubscription> onLiveProcessingStarted, bool resolveLinkTos = false,
            Action<IStreamSubscription, SubscriptionDroppedReason, Exception> subscriptionDropped = null,
            Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            TcpSubscription subscription = null;
            long? lastCheckpoint = start.ToUInt64() == 0 ? null : (long?)start.ToUInt64();
            var result = _client.SubscribeToStreamFrom(streamName, lastCheckpoint,
                CatchUpSubscriptionSettings.Default,
                async (s, r) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Stop);

                    await eventAppeared?.Invoke(subscription,
                        new ResolvedEvent(r.Event.Convert(r.OriginalPosition), r.Link.Convert(r.OriginalPosition), r.OriginalPosition.ConvertCommit()),
                        CancellationToken.None);
                },
                s =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Stop);
                    onLiveProcessingStarted?.Invoke(subscription);
                },
                async (s, d, e) =>
                {
                    if (subscription == null)
                        subscription = new TcpSubscription(s.Stop);
                    subscriptionDropped?.Invoke(subscription, d.Convert(), e);
                });
            if (subscription == null)
                subscription = new TcpSubscription(result.Stop);
            return subscription;
        }

        public async Task<DeleteResult> TombstoneAsync(string streamName, StreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<DeleteResult> TombstoneAsync(string streamName, AnyStreamRevision expectedRevision, Action<EventStoreClientOperationOptions> configureOperationOptions = null,
            UserCredentials userCredentials = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public EventStorePersistentSubscriptionsClient PersistentSubscriptions { get; }
        public EventStoreProjectionManagerClient ProjectionsManager { get; }
        public EventStoreUserManagerClient UsersManager { get; }
    }
}