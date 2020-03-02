using System;
using Carter;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.Fresh;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenUi.Tests.Model.Hotel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace EventDrivenUi.Tests
{
    public class Startup
    {
        private readonly IServiceProvider _serviceProvider;
        private IServiceCollection _services;

       
        public void ConfigureServices(IServiceCollection services)
        {
            this._services = services;
            var config = services.AddEventDrivenThinking(Logger.None, configuration =>
            {
                configuration.AddAssemblies(typeof(Startup).Assembly);
            });

            services.AddEventStoreTesting();
            services.AddCarter(configurator: config.GetCarterConfigurator());
            services.AddSignalR()
                .AddNewtonsoftJsonProtocol();
            
        }
        public void Configure(IApplicationBuilder app)
        {
            ServiceProvider = app.ApplicationServices;

            app.UseRouting();

            ServerPipelineBuilder c = new ServerPipelineBuilder(app.ApplicationServices);

            var w = c.Slices()
                .SendCommands(ServerApp.Instance.ReceiveCommandPipe)
                .WritesEvents(ServerApp.Instance.WriteEventPipe);

            if (ServerApp.Instance.SubscribePipe != null) w.Subscribes(ServerApp.Instance.SubscribePipe);
            c.Build();

            app.UseEndpoints(builder =>
            {
                builder.MapHub<EventStoreHub>("/EventHub");
                builder.MapCarter();
            });

            // Should simplify
            app.ApplicationServices.GetService<IEventStoreHubInitializer>().Init();
        }

        public static IServiceProvider ServiceProvider { get; set; }


        /// <summary>
        /// Ugly workaround
        /// </summary>
        public static Action<CarterConfigurator> HostConfig {  set; get; }
    }
}