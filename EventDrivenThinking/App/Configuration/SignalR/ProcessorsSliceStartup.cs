using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.SignalR
{
    class ProcessorsSliceStartup : IProcessorSliceStartup
    {
        private IProcessorSchema[] _processors;
        private string url;

        public ProcessorsSliceStartup(string url)
        {
            this.url = url;
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<SignalRSubscriber>();
            serviceCollection.TryAddSingleton((sp) => CreateConnection());
        }

        private HubConnection CreateConnection()
        {
            return new HubConnectionBuilder()
                .WithUrl(url)
                .AddNewtonsoftJsonProtocol()
                .WithAutomaticReconnect()
                .Build();
        }

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            var connection = serviceProvider.GetRequiredService<HubConnection>();
            return ActivatorUtilities.GetServiceOrCreateInstance<SignalRSubscriber>(serviceProvider)
                .SubscribeFromEventStream(connection, true,_processors.SelectMany(x => x.Events));
        }

        public void Initialize(IEnumerable<IProcessorSchema> processors)
        {
            this._processors = processors.ToArray();
        }
    }
}