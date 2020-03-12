using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.SessionManagement;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.Integrations.SignalR
{
    public class EventStoreHub : Hub
    {
        
        class Session : ISession
        {
            private readonly string _connectionId;
            private readonly IHubContext<EventStoreHub> _context;
            private ILogger _logger;
            public Session(Guid id, string connectionId, IHubContext<EventStoreHub> context, ILogger logger)
            {
                Id = id;
                _connectionId = connectionId;
                _context = context;
                _logger = logger;
                Subscriptions = new List<Subscription>();
                IsValid = true;
            }
            public void RegisterSubscriptionForEvent(Type eventType)
            {
                _logger.Information("Subscription configured for handling a persistent {eventName}", eventType.Name);
                Subscriptions.Add(new Subscription("unkown", eventType));

            }
            public bool IsValid { get; private set; }
            public Guid Id { get; }
            public ICollection<Subscription> Subscriptions { get; set; }
            public async Task SendEventCore(EventMetadata m, IEvent ev)
            {
                string evName = ev.GetType().FullName.Replace(".","-");
                _logger.Information("Invoking method {methodName} on group {groupId}", evName, Id);
                await _context.Clients.Group(Id.ToString()).SendAsync(evName, m, ev);
            }
        }
        private readonly IHubContext<EventStoreHub> _context;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IClientSessionRegister _register;

        public EventStoreHub(IHubContext<EventStoreHub> context,
            ISessionManager sessionManager, 
            Serilog.ILogger logger,
            IClientSessionRegister register)
        {
            _context = context;
            _sessionManager = sessionManager;
            _logger = logger;
            _register = register;
            
        }

        public async Task Register(Guid sessionId)
        {
            _sessionManager.Register(sessionId, new Session(sessionId, Context.ConnectionId,_context, _logger));
            _register.Map(this.Context.ConnectionId, sessionId);
            _logger.Information("Session {sessionId} registered.", sessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        }

        public async Task SubscribeToProjection(string projectionName)
        {
            // we need to use the subscriptionId to transfer old events.
            // and than add to appropriate group 

        }
        public async Task SubscribeToProjectionPartition(string projectionName, Guid partition)
        {
            // we need to use the subscriptionId to transfer old events.
            // and than add to appropriate group 
        }
        public async Task SubscribeToEvent(bool isPersistent, string eventName)
        {
            var sessionId = _register[this.Context.ConnectionId];
            var session = _sessionManager[sessionId];
            Type eventType = Type.GetType(eventName);
            if(isPersistent)
                session.RegisterSubscriptionForEvent(eventType);

            _logger.Information("EventHub client subscribed to {eventName}", eventType.Name);
            await Groups.AddToGroupAsync(Context.ConnectionId, eventType.FullName);
        }

        public Task Unsubscribe(string eventName)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, eventName);
        }
    }

}
