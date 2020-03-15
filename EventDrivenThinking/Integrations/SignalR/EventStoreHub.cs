using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.SessionManagement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.Integrations.SignalR
{
    public enum StreamPosition : long
    {
        Beginning = -1,
        Live = -2
    }

    public class StreamNotFoundException : Exception
    {
        public StreamNotFoundException(string name) : base($"Stream {name} was not found.")
        {
            
        }
    }

   
    public static class ExpressionExtensions
    {
        public static MethodInfo TryGetGenericMethodCallFrom<TDelegate>(this Expression<TDelegate> expression)
        {
            var mthExpression = (MethodCallExpression)expression.Body;
            return mthExpression.Method.GetGenericMethodDefinition();
        }
    }

    public interface IProjectionStreamTransmitter
    {
        Task Transmit(ISession session, Guid requestId);
    }
    class ProjectionStreamTransmitter<TProjection> : IProjectionStreamTransmitter where TProjection : IProjection
    {
        private IProjectionEventStream<TProjection> _stream;

        public ProjectionStreamTransmitter(IProjectionEventStream<TProjection> stream)
        {
            _stream = stream;
        }

        public async Task Transmit(ISession session, Guid requestId)
        {
            await foreach (var i in _stream.Get())
            {
                await session.SendEventCore(i.Metadata, i.Event);
            }
        }
    }
    public class ProjectionStreamTransmitter
    {
        private readonly IQuerySchemaRegister _register;
        private readonly IServiceProvider _serviceProvider;
        
        public ProjectionStreamTransmitter(IQuerySchemaRegister register, IServiceProvider serviceProvider)
        {
            _register = register;
            _serviceProvider = serviceProvider;
        }

        public async Task TransmitData(ISession receiver, Guid requestId, string streamName)
        {
            var queryInfo = _register.FirstOrDefault(x => x.Category == streamName);
            if(queryInfo == null)
                throw new StreamNotFoundException(streamName);

            using (var scope = _serviceProvider.CreateScope())
            {
                 var transmitter = (IProjectionStreamTransmitter)ActivatorUtilities.CreateInstance(scope.ServiceProvider,
                    typeof(ProjectionStreamTransmitter<>).MakeGenericType(queryInfo.ProjectionType));
                 await transmitter.Transmit(receiver, requestId);
            }
        }

    }
    public class EventStoreHub : Hub
    {
        
        class Session : ISession
        {
            private readonly string _connectionId;
            private readonly IHubContext<EventStoreHub> _context;
            private readonly ILogger _logger;
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

            public Task SendEvents(Guid reqId, IEnumerable<EventEnvelope> events)
            {
                throw new NotImplementedException();
            }

           
        }
        private readonly IHubContext<EventStoreHub> _context;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IClientSessionRegister _register;
        private readonly IServiceProvider _serviceProvider;
        public EventStoreHub(IHubContext<EventStoreHub> context,
            ISessionManager sessionManager, 
            Serilog.ILogger logger,
            IClientSessionRegister register, IServiceProvider serviceProvider)
        {
            _context = context;
            _sessionManager = sessionManager;
            _logger = logger;
            _register = register;
            _serviceProvider = serviceProvider;
        }

        public async Task Register(Guid sessionId)
        {
            _sessionManager.Register(sessionId, new Session(sessionId, Context.ConnectionId,_context, _logger));
            _register.Map(this.Context.ConnectionId, sessionId);
            _logger.Information("Session {sessionId} registered.", sessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        }

        public async Task SubscribeToProjection(Guid requestId, long position, string categoryName)
        {
            // we need to use the subscriptionId to transfer old events.
            // and than add to appropriate group 
            var sessionId = _register[this.Context.ConnectionId];
            var session = _sessionManager[sessionId];
            var projectionStreamName = categoryName + "Projection";
            if (position == (long)StreamPosition.Beginning)
            {
                var transmitter = ActivatorUtilities.CreateInstance<ProjectionStreamTransmitter>(_serviceProvider);
                await transmitter.TransmitData(session, requestId, categoryName);
                await Groups.AddToGroupAsync(Context.ConnectionId, projectionStreamName);
            }
            else
                await Groups.AddToGroupAsync(Context.ConnectionId, projectionStreamName);
        }
        public async Task SubscribeToProjectionPartition(string startFromBeginning, string categoryName, Guid partition)
        {
            // we need to use the subscriptionId to transfer old events.
            // and than add to appropriate group 
        }
        public async Task SubscribeToEvent(Guid requestId, bool isPersistent, string eventName)
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
