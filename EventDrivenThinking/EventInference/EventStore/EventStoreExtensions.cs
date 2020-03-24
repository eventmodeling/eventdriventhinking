using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
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
        
        
        public static IEventStoreFacade BindToSignalHub(this IEventStoreFacade connection,
            IProjectionSchemaRegister projectionSchema,
            IHubContext<EventStoreHub> hubConnection, Serilog.ILogger logger)
        {
            foreach (var e in projectionSchema.Events)
            {
                var configuratorType = typeof(SignalRConfigurator<>).MakeGenericType(e);
                var configurator = Ctor<ISignalRConfigurator>.Create(configuratorType);
                configurator.Configure(connection, hubConnection, logger);
            }

            return connection;
        }

        /// <summary>
        /// Pushes event from event-store to signal-r clients
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        private class SignalRConfigurator<TEvent> : ISignalRConfigurator
            where TEvent : IEvent
        {
            private IHubContext<EventStoreHub> _connection;
            private Serilog.ILogger _logger;

            public void Configure(IEventStoreFacade connection,
                IHubContext<EventStoreHub> hubConnection, Serilog.ILogger logger)
            {
                _logger = logger;
                _logger.Information("Subscribed for {eventName} for pushing to signalR clients.", typeof(TEvent).Name);
                var stream = $"$et-{typeof(TEvent).Name}";
                this._connection = hubConnection;
                var t = Task.Run(() => connection.SubscribeToStreamAsync(stream, OnReadEvent, true));
                t.Wait();
            }
            private async Task OnReadEvent(IStreamSubscription arg1, ResolvedEvent arg2, CancellationToken t)
            {
                var eventData = Encoding.UTF8.GetString(arg2.Event.Data);
                var metaData = Encoding.UTF8.GetString(arg2.Event.Metadata);

                var ev = JsonConvert.DeserializeObject<TEvent>(eventData);
                var m = JsonConvert.DeserializeObject<EventMetadata>(metaData);

                var groupName = typeof(TEvent).FullName.Replace(".","-");
                try
                {
                    await _connection.Clients.All.SendCoreAsync(groupName, new object[]{ m, ev});
                    _logger.Information("SignalR hub send event {eventName} to it's clients.", typeof(TEvent).Name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private interface ISignalRConfigurator
        {
            void Configure(IEventStoreFacade aggregator, IHubContext<EventStoreHub> hubConnection, Serilog.ILogger logger);

        }
        
        
       
    }
}