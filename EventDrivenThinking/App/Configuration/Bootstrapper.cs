using System;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.QueryProcessing;
using EventDrivenThinking.EventInference.SessionManagement;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.Carter;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenThinking.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace EventDrivenThinking.App.Configuration
{
    public class Bootstrapper : IBootstrapper
    {
        private readonly IServiceCollection _collection;
        private readonly Configuration _config;
        public Configuration Configuration => _config;
        internal Bootstrapper(ILogger logger, IServiceCollection collection)
        {
            _collection = collection;
            _config = new Configuration(logger);
        }

        public void Register(Action<Configuration> config)
        {
            RegisterServices();

            config(_config);
            _config.Slices.Register(_collection);
        }

        private void RegisterServices()
        {
            _collection.AddSingleton<IBootstrapper>(this);
            _collection.TryAddSingleton<IModelFactory, ModelFactory>();
            _collection.TryAddSingleton<IQueryInvoker, QueryInvoker>();


            // common registrations
            _collection.TryAddTransient(typeof(IProjectionExecutor<>), typeof(ProjectionExecutor<>));
            _collection.TryAddSingleton(typeof(IQueryEngine<>), typeof(QueryEngine<>));
            
            _collection.TryAddSingleton(typeof(ICheckpointRepository<,>),typeof(FileCheckpointRepository<,>));

            _collection.TryAddSingleton(sp => _config.Services.AggregateSchemaRegister);
            _collection.TryAddSingleton(sp => _config.Services.ProjectionSchemaRegister);
            _collection.TryAddSingleton(sp => _config.Services.ProcessorSchemaRegister);
            _collection.TryAddSingleton(sp => _config.Services.CommandsRegister);
            _collection.TryAddSingleton(sp => _config.Services.QuerySchemaRegister);

            _collection.TryAddSingleton<IProjectionEventStreamRepository, ProjectionEventStreamRepository>();
            _collection.TryAddSingleton<IProjectionSubscriptionController, ProjectionSubscriptionController>();
            _collection.TryAddSingleton<IProjectionStreamSubscriptionController, ProjectionStreamSubscriptionController>();


            _collection.TryAddSingleton<ICommandDispatcher, CommandDispatcher>();
            _collection.TryAddSingleton<IEventHandlerDispatcher, EventHandlerDispatcher>();
            _collection.TryAddSingleton<IEventDataFactory, EventDataFactory>();
            _collection.TryAddSingleton<IClientSessionRegister, ClientSessionRegister>();
            _collection.TryAddSingleton<IEventConverter, EventConverter>();
            _collection.TryAddSingleton<ISessionManager, SessionManager>();
            _collection.TryAddScoped<SessionContext>();
            _collection.TryAddScoped<IHttpSessionManager>(sp => sp.GetRequiredService<SessionContext>());
            _collection.TryAddScoped<ISessionContext>(sp => sp.GetRequiredService<SessionContext>());
            _collection.TryAddSingleton<IEventStoreHubInitializer, EventStoreHubInitializer>();
        }

        public async Task Configure(IServiceProvider provider)
        {
            await _config.Slices.Configure(provider);

        }
    }
}