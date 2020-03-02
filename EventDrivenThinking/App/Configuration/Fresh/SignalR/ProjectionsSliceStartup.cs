using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.Client;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prism.Regions;

namespace EventDrivenThinking.App.Configuration.Fresh.SignalR
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

        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            var connection = serviceProvider.GetRequiredService<HubConnection>();
            return ActivatorUtilities.GetServiceOrCreateInstance<SignalRSubscriber>(serviceProvider)
                .SubscribeAll(connection, true, _projections.SelectMany(x => x.Events));
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}
