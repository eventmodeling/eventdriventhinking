using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Carter;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.Carter;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.Logging;
using EventDrivenThinking.SimpleWebApp.Views.User;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace EventDrivenThinking.SimpleWebApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IEventStoreFacade>((serviceProvider) =>
            {
                var connection = new EventStoreFacade("https://localhost:2113", "tcp://localhost:1113", "admin", "changeit");
                return connection;
            });

            ILogger log = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Debug()
                .WriteTo.Console()
                .CreateLogger();
            EventDrivenThinking.Logging.LoggerFactory.Init(log);
            services.AddSingleton<ILogger>(log);
            
            var config = services.AddEventDrivenThinking(Logger.None, x =>
            {
                x.AddAssemblies(typeof(Startup).Assembly);
                x.Slices.SelectAll()
                    .Queries.FromEventStore()
                    .Events.UseEventStore()
                    .Aggregates.BindCarter().WriteToEventStore()
                    .Processors.SubscribeFromEventStore()
                    .Projections.UseEventStore();
            });
            services.AddCarter(configurator: config.GetCarterConfigurator());
            services.AddSignalR();

            services.AddSingleton<UserModel>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.ApplicationServices.ConfigureEventDrivenThinking();
            app.UseEndpoints(builder =>
            {
                builder.MapCarter();
            });
            
        }
    }
}
