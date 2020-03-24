using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Reflection;
using EventStore.Client;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    public class EventStoreSubscriber
    {
        private readonly IEventStoreFacade _connection;
        private readonly IEventHandlerDispatcher _dispatcher;
        private readonly Serilog.ILogger _logger;

        public EventStoreSubscriber(IEventStoreFacade connection, IEventHandlerDispatcher dispatcher, ILogger logger)
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
            Task Configure(IEventStoreFacade aggregator, IEventHandlerDispatcher dispatcher, Serilog.ILogger logger);
        }
        private class EventHandlerConfigurator<TEvent> : IEventHandlerConfigurator
            where TEvent : IEvent
        {
            private IEventHandlerDispatcher _dispatcher;
            private Serilog.ILogger _logger;
            public async Task Configure(IEventStoreFacade connection, IEventHandlerDispatcher dispatcher, Serilog.ILogger logger)
            {
                _logger = logger;
                _logger.Information("Subscribed for {eventName} for local projections. (with EventDispatcher)", typeof(TEvent).Name);

                var stream = $"$et-{typeof(TEvent).Name}";
                this._dispatcher = dispatcher;
                
                // Should we wait for the subscription? - or should we re-subscribe
                await connection.SubscribeToStreamAsync(stream, OnReadEvent, true);
            }

            
            private async Task OnReadEvent(IStreamSubscription arg1, ResolvedEvent arg2, CancellationToken t)
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