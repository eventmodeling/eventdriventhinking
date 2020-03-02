using System;
using CommonServiceLocator;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.Client;
using EventDrivenThinking.App.Configuration.Fresh;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenThinking.Integrations.Unity.Client;
using EventDrivenThinking.Ui;
using EventDrivenUi.Tests.Model.Projections;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Unity;
using Serilog.Core;
using Unity;
using SendCommandPipe = EventDrivenThinking.App.Configuration.Client.SendCommandPipe;

namespace EventDrivenUi.Tests
{
    
    class ClientApp : IApp, IDisposable
    {
        private readonly IUnityContainer _container;
        private IPipelineBuilder _pipelineBuilder;
        public Action<SubscribePipe> SubscribePipe;
        public Action<SendCommandPipe> SendCommandPipe;
        // send

        public ClientApp(IUnityContainer container = null)
        {
            if (container == null) container = new UnityContainer();
            this._container = container;
        }
        

        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
        public void InitializeContainer(IServiceCollection collection = null)
        {
            if(collection == null)
                collection = new UnityServiceCollection(_container);

            var assemblies = typeof(ClientApp).Assembly;

            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocatorAdapter(_container));
            var serviceProvider = new UnityServiceProvider(_container);

            collection.AddSingleton<IServiceProvider>(serviceProvider);

            // Should add only for Client!
            collection.AddEventDrivenThinking(Logger.None,
                config => config.AddAssemblies(assemblies));

            collection.AddSingleton<IRoomAvailabilityModel, RoomAvailabilityModel>()
                .AddSingleton<IEventAggregator, EventAggregator>()
                .AddSingleton<IServiceDiscovery, InternalDiscoService>()
                .AddSingleton<IHttpClient, HttpAppClient>()
                .AddSingleton<IUiEventBus, UiEventBus>();

        }
        public void ConfigurePlumbing()
        {
            _pipelineBuilder = _container.ConfigureClient(c =>
            {
                var s = c.Slices()
                    .SendCommands(SendCommandPipe);
                if(SubscribePipe != null)
                    s.Subscribes(SubscribePipe);
            });
        }

        public void ConnectPipes()
        {
            _pipelineBuilder.Build();
        }

        public void Dispose()
        {
            _container?.Dispose();
        }
    }
}