using EventStore.ClientAPI.SystemData;

namespace EventDrivenThinking.EventInference.EventStore
{
    public class CredentialManager : ICredentialManager
    {
        public CredentialManager()
        {
            Credentials = new UserCredentials("admin", "changeit");
        }

        public void Save(string userName, string password)
        {
            Credentials = new UserCredentials(userName, password);
        }

        public UserCredentials Credentials { get; private set; }
    }
}