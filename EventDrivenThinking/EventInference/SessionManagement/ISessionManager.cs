using System;

namespace EventDrivenThinking.EventInference.SessionManagement
{
    public interface ISessionManager
    {
        ISession this[Guid sessionId] { get; }
        void Register(Guid sessionId, ISession session);
        void DeRegister(Guid sessionId);
    }
}