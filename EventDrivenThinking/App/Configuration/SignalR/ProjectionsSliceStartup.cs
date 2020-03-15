using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.SignalR
{
    class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private IProjectionSchema[] _projections;
        private string url;

        public ProjectionsSliceStartup(string url)
        {
            this.url = url;
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<ISubscriptionManager, EventAggregatorSubscriptionManager>();
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

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            var connection = serviceProvider.GetRequiredService<HubConnection>();
            foreach(var i in _projections)
            {
                await ActivatorUtilities.GetServiceOrCreateInstance<SignalRSubscriber>(serviceProvider)
                    .SubscribeFromProjectionStream(connection, true, i.Category, i.Events);

            }
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}
