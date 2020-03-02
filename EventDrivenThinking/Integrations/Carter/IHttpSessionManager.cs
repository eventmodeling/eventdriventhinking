using Microsoft.AspNetCore.Http;

namespace EventDrivenThinking.Integrations.Carter
{
    public interface IHttpSessionManager
    {
        void Read(HttpRequest req);
    }
}