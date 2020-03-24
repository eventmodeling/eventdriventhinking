using EventStore.Client;

namespace EventDrivenThinking.EventInference.EventStore
{
    public interface ICredentialManager
    {
        UserCredentials Credentials { get; }
        void Save(string userName, string password);
    }
}