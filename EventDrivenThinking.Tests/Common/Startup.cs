using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Carter;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.Carter;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using EventDrivenThinking.Example.Model.ReadModels.Hotel;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenThinking.Ui;
using EventStore.Client;
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
                    .Projections.UseEventStore(true)
                    .Processors.SubscribeFromEventStore()
                    .Events.UseEventStore()
                    .Queries.FromEventStore() 
                    .Commands.ToCommandHandler();
            });

            services.AddSingleton<IEventStoreFacade>((serviceProvider) =>
            {
                var connection = new EventStoreFacade("https://localhost:2113", "tcp://localhost:1113", "admin", "changeit");
                return connection;
            });

            /// FOR NOW
            foreach (var modelType in assemblies.SelectMany(x => x.GetTypes())
                .Where(x => typeof(IModel).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface))
                services.AddSingleton(modelType);

            
            foreach (var i in config.Services.QuerySchemaRegister)
            {
                services.AddTransient(
                    typeof(IQueryHandler<,,>).MakeGenericType(i.Type, i.ModelType, i.ResultType), i.QueryHandlerType);

                //foreach (var p in i.StreamPartitioners)
                //    services.AddTransient(typeof(IQueryPartitioner<>).MakeGenericType(i.Type), p);
                
                foreach (var p in i.QueryPartitioners)
                    services.AddTransient(typeof(IQueryPartitioner<>).MakeGenericType(i.Type), p);
            }

            services.AddSingleton<DispatcherQueue>();
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
            ServiceProvider = app.ApplicationServices;
        }

        public static IServiceProvider ServiceProvider;
    }
}