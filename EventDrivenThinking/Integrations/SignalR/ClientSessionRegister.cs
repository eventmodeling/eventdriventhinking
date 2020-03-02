using System;
using System.Collections.Concurrent;

namespace EventDrivenThinking.Integrations.SignalR
{
    public interface IClientSessionRegister
    {
        Guid this[string clientId] { get; }
        void Map(string clientId, Guid sessionId);
    }

    public class ClientSessionRegister : IClientSessionRegister
    {
        private readonly ConcurrentDictionary<string, Guid> _clientSessionIndex;

        public ClientSessionRegister()
        {
            _clientSessionIndex = new ConcurrentDictionary<string, Guid>();
        }

        public Guid this[string clientId]
        {
            get { return _clientSessionIndex[clientId]; }
        }

        public void Map(string clientId, Guid sessionId)
        {
            _clientSessionIndex.TryAdd(clientId, sessionId);
        }
    }
}