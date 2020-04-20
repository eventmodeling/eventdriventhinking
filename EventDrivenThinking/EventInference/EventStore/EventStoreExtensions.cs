using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenThinking.Reflection;
using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.EventStore
{
    public static class EventStoreExtensions
    {
        
        
        public static async Task BindToSignalHub(this IEventStoreFacade connection,
            IEventConverter eventConverter,
            IProjectionSchemaRegister projectionSchema,
            IHubContext<EventStoreHub> hubConnection, Serilog.ILogger logger)
        {
            foreach (var e in projectionSchema.Events)
            {
                var configuratorType = typeof(SignalRConfigurator<>).MakeGenericType(e);
                var configurator = Ctor<ISignalRConfigurator>.Create(configuratorType);
                await configurator.Configure(connection, eventConverter, hubConnection, logger);
            }
        }

        /// <summary>
        /// Pushes event from event-store to signal-r clients
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        private class SignalRConfigurator<TEvent> : ISignalRConfigurator
            where TEvent : IEvent
        {
            private IHubContext<EventStoreHub> _connection;
            private Serilog.ILogger Log;
            private IEventConverter _converter;

            public async Task Configure(IEventStoreFacade connection, 
                IEventConverter eventConverter,
                IHubContext<EventStoreHub> hubConnection, Serilog.ILogger logger)
            {
                Log = logger;
                _converter = eventConverter;
                var stream = $"$et-{typeof(TEvent).Name}";
                this._connection = hubConnection;
                var t = await connection.SubscribeToStreamAsync(stream, StreamRevision.Start, OnReadEvent, null, true);
                
                Log.Information("Subscribed for {eventName} for pushing to signalR clients.", typeof(TEvent).Name);
            }
            private async Task OnReadEvent(IStreamSubscription arg1, ResolvedEvent arg2, CancellationToken t)
            {
                var (m, ev) = _converter.Convert<TEvent>(arg2);
                var groupName = typeof(TEvent).FullName.Replace(".","-");
                try
                {
                    await _connection.Clients.All.SendCoreAsync(groupName, new object[]{ m, ev});
                    Log.Information("SignalR hub send event {eventName} to it's clients.", typeof(TEvent).Name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private interface ISignalRConfigurator
        {
            Task Configure(IEventStoreFacade aggregator,
                IEventConverter eventConverter,
                IHubContext<EventStoreHub> hubConnection, 
                Serilog.ILogger logger);

        }
        
        
       
    }
}