using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.SessionManagement;
using Microsoft.AspNetCore.Http;
using Serilog;
using ISession = EventDrivenThinking.EventInference.SessionManagement.ISession;

namespace EventDrivenThinking.Integrations.Carter
{
    public class SessionContext : ISessionContext, IHttpSessionManager
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private Session _current;

        public SessionContext(ISessionManager sessionManager, ILogger logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        class Session : ISession
        {
            private readonly ILogger _logger;
            private ISession _proxy;

            public Session(ILogger logger)
            {
                _logger = logger;
            }

            public bool IsValid { get; private set; }
            public Guid Id => _proxy.Id;

            public ICollection<Subscription> Subscriptions => _proxy.Subscriptions;

            public void RegisterSubscriptionForEvent(Type eventType)
            {
               _proxy.RegisterSubscriptionForEvent(eventType);
            }

            public async Task SendEventCore(EventMetadata m, IEvent ev)
            {
                await _proxy.SendEventCore(m, ev);
                _logger.Information("SignalR hub is sending {eventName} on the session {sessionId}", ev.GetType().Name, Id);
            }

            public void Init(ISession session)
            {
                _proxy = session;
                IsValid = true;
            }
        }

        public ISession Current()
        {
            return _current ??= new Session(_logger);
        }
        public void Read(HttpRequest req)
        {
            if (_current == null) _current = new Session(_logger);
            string sessionIdStr = req.Headers["session-id"].FirstOrDefault();
            if (Guid.TryParse(sessionIdStr, out Guid sessionId) && _sessionManager.SessionExists(sessionId))
            {
                _current.Init(_sessionManager[sessionId]);
            }
        }
    }
}