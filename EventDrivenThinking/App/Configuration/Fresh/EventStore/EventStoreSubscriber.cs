using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Reflection;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.App.Configuration.Fresh.EventStore
{
    public class EventStoreSubscriber
    {
        private readonly IEventStoreConnection _connection;
        private readonly IEventHandlerDispatcher _dispatcher;
        private readonly Serilog.ILogger _logger;

        public EventStoreSubscriber(IEventStoreConnection connection, IEventHandlerDispatcher dispatcher, ILogger logger)
        {
            this._connection = connection;
            this._dispatcher = dispatcher;
            this._logger = logger;
        }

        public async Task SubscribeAll(IEnumerable<Type> events)
        {
            foreach (var e in events)
            {
                var configuratorType = typeof(EventHandlerConfigurator<>).MakeGenericType(e);
                var configurator = Ctor<IEventHandlerConfigurator>.Create(configuratorType);
                await configurator.Configure(_connection, _dispatcher, _logger);
            }
        }
        private interface IEventHandlerConfigurator
        {
            Task Configure(IEventStoreConnection aggregator, IEventHandlerDispatcher dispatcher, Serilog.ILogger logger);
        }
        private class EventHandlerConfigurator<TEvent> : IEventHandlerConfigurator
            where TEvent : IEvent
        {
            private IEventHandlerDispatcher _dispatcher;
            private Serilog.ILogger _logger;
            public async Task Configure(IEventStoreConnection connection, IEventHandlerDispatcher dispatcher, Serilog.ILogger logger)
            {
                _logger = logger;
                _logger.Information("Subscribed for {eventName} for local projections. (with EventDispatcher)", typeof(TEvent).Name);

                var stream = $"$et-{typeof(TEvent).Name}";
                this._dispatcher = dispatcher;
                
                // Should we wait for the subscription? - or should we re-subscribe
                await connection.SubscribeToStreamAsync(stream, true, OnReadEvent);
            }

            

            private async Task OnReadEvent(EventStoreSubscription arg1, ResolvedEvent arg2)
            {
                
                var eventData = Encoding.UTF8.GetString(arg2.Event.Data);
                var metaData = Encoding.UTF8.GetString(arg2.Event.Metadata);

                var ev = JsonConvert.DeserializeObject<TEvent>(eventData);
                var m = JsonConvert.DeserializeObject<EventMetadata>(metaData);

                _logger.Information("EventDispatcher is receiving {eventName}.", typeof(TEvent).Name);
                await _dispatcher.Dispatch(m, ev);
            }
        }
    }
}