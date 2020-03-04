﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Carter;
using EventDrivenThinking.App.Configuration.Fresh;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.Example.Model.Hotel;
using EventDrivenThinking.Example.Model.Projections;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenThinking.Ui;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.Tests.Common
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //this.sentry = Sentry.SentrySdk.Init( opt => opt.Dsn = new Dsn("http://949295c75e454b578e5336dce15d4af4@localhost:9000/1"));

            ILogger log = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Debug()
                .CreateLogger();

            log.Information("Configuring services...");
            Assembly[] assemblies = new[]
            {
                typeof(HotelAggregate).Assembly
            };
            services.AddSingleton<ILogger>(log);
            var config = services.AddEventDrivenThinking(log, c =>
            {
                c.AddAssemblies(assemblies);

                c.Slices.SelectAll()
                    .Aggregates.BindCarter().WriteToEventStore()
                    .Projections.SubscribeFromEventStore()
                    .Processors.SubscribeFromEventStore()
                    .CommandInvocations.ToCommandHandler();
            });

            services.AddSingleton((serviceProvider) =>
            {
                var connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
                connection.ConnectAsync().GetAwaiter().GetResult();

                return connection;
            });

            /// FOR NOW
            foreach (var modelType in assemblies.SelectMany(x => x.GetTypes())
                .Where(x => typeof(IModel).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface))
                services.AddSingleton(modelType);

            services.AddSingleton<IRoomAvailabilityModel, RoomAvailabilityModel>();
            services.AddSingleton<IModelFactory, ModelFactory>();

            services.AddCarter(configurator: config.GetCarterConfigurator());
            services.AddSignalR()
                .AddNewtonsoftJsonProtocol();

            log.Information("Configuring done.");
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var logger = app.ApplicationServices.GetService<ILogger>();
            logger.Information("Starting...");
            app.UseRouting();

            app.ApplicationServices.ConfigureEventDrivenThinking();

            app.UseEndpoints(builder =>
            {
                builder.MapHub<EventStoreHub>("/EventHub");
                builder.MapCarter();
            });

            app.ApplicationServices.GetService<IEventStoreHubInitializer>().Init();
            logger.Information("Started.");

        }
    }
}