using System;
using System.Collections.Concurrent;

namespace EventDrivenThinking.EventInference.SessionManagement
{
    /// <summary>
    /// Mostly used by signalr sessions? 
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<Guid, ISession> _sessions;

        public SessionManager()
        {
            _sessions = new ConcurrentDictionary<Guid, ISession>();
        }
        public ISession this[Guid sessionId] => _sessions[sessionId];

        public void Register(Guid sessionId, ISession session)
        {
            _sessions.TryAdd(sessionId, session);
        }

        public void DeRegister(Guid sessionId)
        {
            _sessions.TryRemove(sessionId, out ISession session);
        }
    }
}