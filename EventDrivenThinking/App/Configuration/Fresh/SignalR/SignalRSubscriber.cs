using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Reflection;
using EventDrivenThinking.Ui;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace EventDrivenThinking.App.Configuration.Fresh.SignalR
{
    public class SignalRSubscriber
    {
        public SignalRSubscriber(IUiEventBus dispatcher, IClientSession session, ILogger logger)
        {
            this._dispatcher = dispatcher;
            this._session = session;
            this._logger = logger;
            _subscribedEvents = new HashSet<Type>();
        }

        private readonly HashSet<Type> _subscribedEvents;
        private readonly IUiEventBus _dispatcher;
        private readonly IClientSession _session;
        private readonly ILogger _logger;
        
        public async Task SubscribeAll(HubConnection connection, bool isPersistent, IEnumerable<Type> eventTypes)
        {
            await CheckConnection(connection);

            foreach (var e in eventTypes.Where(x=> !_subscribedEvents.Contains(x)))
            {
                _subscribedEvents.Add(e);
                var configuratorType = typeof(EventHandlerConfigurator<>).MakeGenericType(e);
                var configurator = Ctor<IEventHandlerConfigurator>.Create(configuratorType);
                await configurator.Configure(isPersistent, connection, _dispatcher, _logger);
            }
        }

        private async Task CheckConnection(HubConnection connection)
        {
            if (connection.State != HubConnectionState.Connected)
            {
                _logger.Information("Connecting to SignalR Hub");
                await connection.StartAsync();
                await connection.InvokeAsync("Register", _session.Id);
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                connection.Closed += async (c) => Debug.WriteLine("Connection lost");
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            }
        }

        private interface IEventHandlerConfigurator
        {
            Task Configure(bool isPersistent, HubConnection aggregator, IUiEventBus serviceProvider, ILogger logger);
        }

        private class EventHandlerConfigurator<TEvent> : IEventHandlerConfigurator
            where TEvent : IEvent
        {
            private IUiEventBus _uiEventBus;
            private ILogger _logger;


            public async Task Configure(bool isPersistent, HubConnection connection, IUiEventBus uiEventBus, ILogger logger)
            {
                _uiEventBus = uiEventBus;
                _logger = logger;
                var eventName = typeof(TEvent).FullName.Replace(".", "-");

                connection.On(eventName, new Action<EventMetadata, TEvent>(OnReadEvent));
                // Should we wait for the subscription? - or should we re-subscribe
                logger.Information("Subscribing to {eventName} with method {methodName}", typeof(TEvent).Name, eventName);
                await connection.InvokeAsync("Subscribe", isPersistent, typeof(TEvent).AssemblyQualifiedName);
                logger.Information("Subscribed to {eventName}.", typeof(TEvent).Name);
            }

            private void OnReadEvent(EventMetadata m, TEvent e)
            {
                EventEnvelope<TEvent> envelope = new EventEnvelope<TEvent>(e, m);
                _logger.Information("Received event {eventName} from SignalR hub.", envelope.Event.GetType().Name);
                _uiEventBus.GetEvent<EventEnvelope<TEvent>>().Publish(envelope);
            }

        }
    }
}