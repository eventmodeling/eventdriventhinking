using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Serilog;

namespace EventDrivenThinking.EventInference.Client
{
    public interface IClientSession
    {
        Guid Id { get; }
    }
    public class ClientSession : IClientSession
    {
        public Guid Id { get; private set; }

        public ClientSession()
        {
            Id = Guid.NewGuid();
        }
    }

    // We could make a httpClient pool as well :)
    public class HttpAppClient : IHttpClient
    {
        private readonly IClientSession _session;
        private readonly ILogger _logger;

        private readonly ActionBlock<IAggregateOperation>[] buffer;
        // We need to add auth-headers etc...
        public HttpAppClient(IClientSession session, ILogger logger)
        {
            _session = session;
            _logger = logger;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("session-id", session.Id.ToString());

            buffer = new ActionBlock<IAggregateOperation>[16];
            for(int i = 0; i < buffer.Length; i++) 
                buffer[i] = new ActionBlock<IAggregateOperation>(x => x.Run(), new ExecutionDataflowBlockOptions() { EnsureOrdered = true });
        }
        private readonly HttpClient _client;
        

        interface IAggregateOperation
        {
            Task Run();
        }
        class PostAggregateOperation<T> : IAggregateOperation
        {
            private readonly T Arg;
            private readonly Guid Id;
            private readonly string Url;
            private readonly HttpClient _client;
            private readonly ILogger _logger;

            public PostAggregateOperation(T arg, Guid id, string url, HttpClient client, ILogger logger)
            {
                Arg = arg;
                Id = id;
                Url = url;
                _client = client;
                _logger = logger;
            }

            public async Task Run()
            {
                var content = JsonConvert.SerializeObject(Arg);
                _logger.Information("Invoking {url}/{aggregateId}", Url, Id);
                var results = await _client.PostAsync($"{Url}/{Id}", new StringContent(content));

                if (!results.IsSuccessStatusCode)
                    throw new Exception("Hymmm we need to do something about it...");
            }
        }
        
        public async Task PostAsync<T>(string url, Guid aggregateId, T command)
        {
            var index = Math.Abs(aggregateId.GetHashCode() % (buffer.Length-1));
            await buffer[index].SendAsync(new PostAggregateOperation<T>(command, aggregateId, url, _client, _logger));
        }

        
    }
}