using System;
using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Client
{
    public interface IHttpClient
    {
        Task PostAsync<T>(string url, Guid aggregateId, T command);
    }
}