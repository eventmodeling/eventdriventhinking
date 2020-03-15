using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Nito.AsyncEx;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    interface IStreamReceiver
    {
        Task Subscribe(IEventStoreConnection connection);
        
    }

    class StreamEventReceiver<TEvent> : IStreamReceiver where TEvent : IEvent
    {
        private readonly string _streamName;
        private readonly StreamJoinCoordinator _coordinator;
        private long? _lastCheckpoint;
        private readonly int _number;
        private readonly ICheckpointEventRepository<TEvent> _checkpointRepo;
        private readonly CatchUpSubscriptionSettings _settings;
        private EventStoreStreamCatchUpSubscription Subscription { get; set; }
        private int _reconnectionCounter = 0;
        private IEventStoreConnection _connection;

        public StreamEventReceiver(StreamJoinCoordinator coordinator, string streamName, int number, ICheckpointEventRepository<TEvent> checkpointRepo)
        {
            _streamName = streamName;
            _coordinator = coordinator;
            _number = number;
            _checkpointRepo = checkpointRepo;
            _settings = new CatchUpSubscriptionSettings(1024, 1024, false, true, coordinator.SubscriptionName);
        }

        public async Task Subscribe(IEventStoreConnection connection)
        {
            _connection = connection;
            _lastCheckpoint = await _checkpointRepo.GetLastCheckpoint();
            Debug.WriteLine($"Subscribing from {_lastCheckpoint ?? -1}");
            this.Subscription = _connection.SubscribeToStreamFrom(_streamName,
                _lastCheckpoint,
                _settings,
                OnEventAppeared,
                OnLiveProcessingStarted, OnSubscriptionDropped);
        }

        private void OnSubscriptionDropped(EventStoreCatchUpSubscription arg1, SubscriptionDropReason arg2, Exception arg3)
        {
            //throw new NotImplementedException("We need to implement reconnect.");
            Debug.WriteLine($"Subscription was dropped {_streamName} (reason = {arg2}, Exception = {arg3}), reconnecting.");
            _reconnectionCounter += 1;
            
            this.Subscription = _connection.SubscribeToStreamFrom(_streamName,
                _lastCheckpoint,
                _settings,
                OnEventAppeared,
                OnLiveProcessingStarted, OnSubscriptionDropped);
            
        }

        private void OnLiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            _coordinator.ReceiverIsLive(_number);
        }

        private async Task OnEventAppeared(EventStoreCatchUpSubscription arg1, ResolvedEvent arg2)
        {
            var eventData = Encoding.UTF8.GetString(arg2.Event.Data);
            var metaData = Encoding.UTF8.GetString(arg2.Event.Metadata);

            var ev = JsonConvert.DeserializeObject<TEvent>(eventData);
            var m = JsonConvert.DeserializeObject<EventMetadata>(metaData);

            _lastCheckpoint = arg2.OriginalEventNumber;
            
            await _coordinator.Push(_number, m, ev);
            await _checkpointRepo.SaveCheckpoint(arg2.OriginalEventNumber);
        }
    }


    public class StreamJoinCoordinator
    {
        class ReceiverNode
        {
            public ReceiverNode(IStreamReceiver receiver, Type eventHandlerType)
            {
                Receiver = receiver;
                EventHandlerType = eventHandlerType;
                ResetEvent = new AsyncAutoResetEvent(false);
                IsLive = false;
            }

            public readonly Type EventHandlerType;
            public readonly IStreamReceiver Receiver;
            public readonly AsyncAutoResetEvent ResetEvent;
            public bool IsLive;
        }
        private ReceiverNode[] _receivers;
        private SortedList<DateTimeOffset, int> _buffer;

        private ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventStoreConnection _connection;
        

        public int Count => _buffer.Count;
        public bool IsLive => _receivers.All(x => x.IsLive);
        public string SubscriptionName { get; private set; }

        
        private int _requiredPastReceiver;
        private int _readyPastCatchupReceivers;
        private DateTimeOffset _lastDispatchedEventTime;

        public StreamJoinCoordinator(IEventStoreConnection connection,  
            Serilog.ILogger logger, IServiceProvider serviceProvider)
        {
            this._connection = connection;
            
            _logger = logger;
            _serviceProvider = serviceProvider;
            _readyPastCatchupReceivers = 0;
        }
        
        public StreamJoinCoordinator WithName(string subscriptionName)
        {
            SubscriptionName = subscriptionName;
            return this;
        }
        public void ReceiverIsLive(int receiverNumber)
        {
            lock (this)
            {
                var node = _receivers[receiverNumber];
                node.IsLive = true;
                _requiredPastReceiver -= 1;
                if (_buffer.Any())
                {
                    var receiverToWakeup = _buffer.Values[0];
                    _receivers[receiverToWakeup].ResetEvent.Set();
                }
            }
        }
        public async Task Push<TEvent>(int receiverNumber, EventMetadata metadata, TEvent ev) where TEvent : IEvent
        {
            bool canDispatch = false;
            var node = _receivers[receiverNumber];
            
            if (_requiredPastReceiver > 0)
                lock (this)
                {
                    if (_requiredPastReceiver > 0)
                    {
                        _buffer.Add(metadata.TimeStamp, receiverNumber); // should handle same timestamp
                        
                        if (!node.IsLive)
                            _readyPastCatchupReceivers += 1;

                        if (_readyPastCatchupReceivers == _requiredPastReceiver)
                        {
                            if (_buffer.Values[0] == receiverNumber)
                            {
                                canDispatch = true;
                                _buffer.Remove(metadata.TimeStamp);
                                if (!node.IsLive)
                                    _readyPastCatchupReceivers -= 1;
                            }
                            else
                            {
                                var receiverToWakeup = _buffer.Values[0];
                                _receivers[receiverToWakeup].ResetEvent.Set();


                            }
                        }
                        else if (metadata.TimeStamp < _lastDispatchedEventTime && node.IsLive)
                        {
                            _buffer.Remove(metadata.TimeStamp);
                            canDispatch = true;
                        }
                    }
                    else // double check
                    {
                        canDispatch = true;
                    }
                }
            else canDispatch = true;

            if (!canDispatch)
            {
                await node.ResetEvent.WaitAsync();
                lock (this)
                {
                    if(_buffer.Values[0] != receiverNumber)
                        throw new InvalidOperationException("Attempt to remove stuff that does not belong to this receiver.");

                    _buffer.Remove(metadata.TimeStamp);
                    if (!node.IsLive)
                        _readyPastCatchupReceivers -= 1;
                }
            }

            _lastDispatchedEventTime = metadata.TimeStamp;

            using(var scope = _serviceProvider.CreateScope())
            {
                 var handler = (IEventHandler<TEvent>)ActivatorUtilities.CreateInstance(scope.ServiceProvider, node.EventHandlerType);
                await handler.Execute(metadata, ev);
            }

            if (node.IsLive && _requiredPastReceiver > 0)
                lock (this)
                {
                    if (node.IsLive && _buffer.Any())
                    {
                        var receiverToWakeup = _buffer.Values[0];
                        _receivers[receiverToWakeup].ResetEvent.Set();
                    }
                }
        }
        public async Task SubscribeToStreams(params SubscriptionInfo[] eventTypes)
        {
            // Load each event in each stream to buffer.
            // soft the buffer
            // start dispatching:
            
            _receivers = new ReceiverNode[eventTypes.Length];
            for (int i = 0; i < eventTypes.Length; i++)
                _receivers[i] = CreateFromEvent(eventTypes[i], i);

            _requiredPastReceiver = _receivers.Length;
            _buffer = new SortedList<DateTimeOffset, int>(_receivers.Length * 2);

            foreach (var i in _receivers)
                await i.Receiver.Subscribe(_connection);
        }
        private ReceiverNode CreateFromEvent(SubscriptionInfo s, int nr)
        {
            var checkpointRepoType = typeof(ICheckpointRepository<,>).MakeGenericType(s.ProjectionType, s.EventType);
            var receiver = (IStreamReceiver) Activator.CreateInstance(
                typeof(StreamEventReceiver<>).MakeGenericType(s.EventType),
                new object[]
                {
                    this, 
                    $"$et-{s.EventType.Name}", 
                    nr++,
                    ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, checkpointRepoType)
                }, null);
            
            return new ReceiverNode(receiver, s.HandlerType);
        }
    }
    public class NullCheckpointRepository<TOwner, TEvent> : ICheckpointRepository<TOwner, TEvent>
    {
        public Task<long?> GetLastCheckpoint()
        {
            return Task.FromResult((long?)null);
        }

        public Task SaveCheckpoint(long checkpoint)
        {
            return Task.CompletedTask;
        }
    }
    public interface ICheckpointEventRepository<TEvent>
    {
        Task<long?> GetLastCheckpoint();
        Task SaveCheckpoint(long checkpoint);
    }
    public interface ICheckpointRepository<TOwner, TEvent> : ICheckpointEventRepository<TEvent>
    {
        
    }

    class FileCheckpointRepository<TOwner, TEvent> : ICheckpointRepository<TOwner, TEvent>
    {
        private readonly string fileName;

        public FileCheckpointRepository()
        {
            this.fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Modellution",
                $"{typeof(TOwner).Name}_{typeof(TEvent).Name}.chk");
        }
        public async Task<long?> GetLastCheckpoint()
        {
            byte[] buffer = new byte[8];
            
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read, 8))
            {
                if(await fs.ReadAsync(buffer, 0, 8) == 8)
                    return BitConverter.ToInt64(buffer, 0);
            }

            return null;

        }

        public async Task SaveCheckpoint(long checkpoint)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 8))
            {
                await fs.WriteAsync(BitConverter.GetBytes(checkpoint), 0, 8);
                await fs.FlushAsync();
            }
        }
    }
    public class SubscriptionInfo
    {
        public readonly Type EventType;
        public readonly Type HandlerType;
        public readonly Type ProjectionType;

        public SubscriptionInfo(Type eventType, Type handlerType, Type projectionType)
        {
            EventType = eventType;
            HandlerType = handlerType;
            ProjectionType = projectionType;
        }
    }
}