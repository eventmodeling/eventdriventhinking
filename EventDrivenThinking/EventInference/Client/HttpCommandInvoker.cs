using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Client
{
    public class HttpCommandInvoker<TCommand> : ICommandInvoker<TCommand> where TCommand : ICommand
    {
        private readonly IHttpClient _httpClient;
        private readonly string _url;

        public HttpCommandInvoker(IHttpClient httpClient, IServiceDiscovery discoSrv)
        {
            this._httpClient = httpClient;
            this._url = discoSrv.Discover(typeof(TCommand));
        }

        public async Task Invoke(Guid id, TCommand cmd)
        {
            await _httpClient.PostAsync(_url, id, cmd);
        }
    }
}