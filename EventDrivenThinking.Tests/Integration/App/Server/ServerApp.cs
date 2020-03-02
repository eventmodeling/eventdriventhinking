using System;
using System.Threading.Tasks;
using AutoMapper.Configuration;
using Carter;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.Fresh;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenUi.Tests.Model.Projections;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Serilog.Core;
using Unity;


namespace EventDrivenUi.Tests
{
    /*
     * Move registrations to configureservices with assembly scanning.
     */


    class ServerApp : IApp, IDisposable
    {
        internal static ServerApp Instance;
        
        private IUnityContainer _container;
        private IServiceProvider _serviceProvider;

        private IPipelineBuilder _pipelineBuilder;
        public Action<SendCommandPipe> ReceiveCommandPipe;
        public Action<WriteEventsPipe> WriteEventPipe;
        public Action<SubscribePipe> SubscribePipe;
        private IWebHost _host;
        

        public ServerApp(IUnityContainer container = null)
        {
            if (container == null) container = new UnityContainer();
            _container = container;
            Instance = this;
        }

        public Action<CarterConfigurator> HostConfig { get; set; }

        public void StartHost()
        {
           _host.StartAsync().GetAwaiter().GetResult();
           _serviceProvider = Startup.ServiceProvider;
        }
        public void ConfigureHost()
        {
            this._host = WebHost
                .CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                //.UseUnityServiceProvider(_container)
                .UseUrls("http://localhost:5000")
                .UseStartup<Startup>()
                .Build();
            
        }
       
        public void ConnectPipes()
        {
            _pipelineBuilder.Build();
        }

        public IServiceProvider ServiceProvider => Startup.ServiceProvider;

        public T Resolve<T>()
        {
            var result = _serviceProvider.GetService<T>();

            if (result == null)
            {
                var serviceScope = _host.Services.CreateScope();
                return serviceScope.ServiceProvider.GetRequiredService<T>();
            }

            return result;
        }

        public void InitializeContainer(IServiceCollection collection = null)
        {
            if(collection == null)
                collection = new UnityServiceCollection(_container);

            this._serviceProvider = new UnityServiceProvider(_container);

            collection.AddSingleton<IServiceProvider>(_serviceProvider);
            collection.TryAddSingleton<IEventAggregator, EventAggregator>();
            collection.AddEventDrivenThinking(Logger.None,
                options => options.AddAssemblies(typeof(ServerApp).Assembly));
            collection.AddScoped<IRoomAvailabilityModel, RoomAvailabilityModel>();
            
        }

        public void ConfigurePlumbing()
        {
            ServerPipelineBuilder c = new ServerPipelineBuilder(_serviceProvider);

            var w = c.Slices()
                .SendCommands(ServerApp.Instance.ReceiveCommandPipe)
                .WritesEvents(ServerApp.Instance.WriteEventPipe);

            if (ServerApp.Instance.SubscribePipe != null) w.Subscribes(ServerApp.Instance.SubscribePipe);
            _pipelineBuilder = c;
        }

        
        public void Dispose()
        {
            _container?.Dispose();
            _host?.Dispose();
        }

        
    }
}