using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.EventStore;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.QueryProcessing;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using EventDrivenThinking.Example.Model.ReadModels.Hotel;
using EventDrivenThinking.Integrations.EventAggregator;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenThinking.Reflection;
using EventDrivenThinking.Ui;
using EventStore.Client;
using EventStore.ClientAPI.Embedded;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Events;
using Serilog.Core;
using Unity;

#pragma warning disable 1998
namespace EventDrivenThinking.Tests.Common
{
    enum ExecutorMode
    {
        Given, When, Then
    }

    public class EventStoreServer
    {
        private ClusterVNode node;
        public static EventStoreServer Instance = new EventStoreServer();
        private EventStoreServer() { }
        public async Task Start()
        {
            var nodeBuilder = EmbeddedVNodeBuilder.AsSingleNode()
                .OnDefaultEndpoints()
                //.WithServerCertificate(null)
                .RunProjections(ProjectionType.System)
                .RunInMemory();
            this.node = nodeBuilder.Build();

            await node.StartAsync(true);
        }

        public async Task Restart()
        {
            await Stop();
            await Start();
        }
        public async Task Stop()
        {
            node?.StopAsync();
        }
    }

    public class AppSpecificationExecutor : ISpecificationExecutor
    {
        class ClientApp
        {
            public readonly IServiceProvider ServiceProvider;
            public readonly IServiceCollection ServiceCollection;

            public ClientApp(IServiceProvider serviceProvider, IServiceCollection serviceCollection)
            {
                ServiceProvider = serviceProvider;
                ServiceCollection = serviceCollection;
                serviceCollection.AddSingleton(serviceProvider);
            }
        }
        class StackFrame
        {
            public ExecutorMode Mode { get; private set; }
            public Stack<(Guid,IEvent)> Events { get; private set; }

            public StackFrame(ExecutorMode mode)
            {
                Mode = mode;
                Events = new Stack<(Guid, IEvent)>();
            }
        }
        private bool _init;
        private ExecutorMode _currentMode = ExecutorMode.Given;
        private async Task CheckInitialized()
        {
            if (!_init)
            {
                _init = true;
                //await EventStoreServer.Instance.Start();
                //await StartEventStoreInDocker();
                await StartWebServer();
                await StartEventStoreProjections();

                await InitClient();
            }
        }

        private async Task InitClient()
        {
            const string url = "http://localhost:5000/EventHub";
            Assembly[] assemblies = new[]
            {
                typeof(HotelAggregate).Assembly
            };
            _client.ServiceCollection.AddSingleton(await GetEventStoreConnection());
            _client.ServiceCollection.AddSingleton(Logger.None);
            _client.ServiceCollection.AddSingleton<IUiEventBus, UiEventBus>();
            _client.ServiceCollection.AddSingleton<IEventAggregator, EventAggregator>();
            _client.ServiceCollection.AddSingleton<IModelFactory, UiModelFactory>();
            _client.ServiceCollection.AddSingleton<IEventStream, InMemoryEventStream>();
            _client.ServiceCollection.AddSingleton<IServiceDiscovery, InternalDiscoService>();
            _client.ServiceCollection.AddSingleton<IHttpClient, HttpAppClient>();
            _client.ServiceCollection.AddSingleton<IClientSession, ClientSession>();

            _client.ServiceCollection.AddTransient<IRoomAvailabilityModel, RoomAvailabilityModel>();

            _client.ServiceCollection.AddEventDrivenThinking(Logger.None,
                config =>
                {
                    config.AddAssemblies(assemblies);
                    config.Slices.SelectAll()
                        //.Projections.NoGlobalHandlers().SubscribeFromEventStore()
                        .Projections.SubscribeFromSignalR(url)
                        .Queries.FromEventStore();
                });

            await _client.ServiceProvider.ConfigureEventDrivenThinking();

        }


        private ClusterVNode node;
        private IWebHost _host;
        private CommandSpecExecutor _executor;
        private ClientApp _client;

        public AppSpecificationExecutor()
        {
            FileCheckpointConfig.Clean();

            _executor = new CommandSpecExecutor();
            _frames = new Stack<StackFrame>();
            _liveQueries = new List<ILiveResult>();
            _frames.Push(new StackFrame(ExecutorMode.Given));
            _queryEngines = new Dictionary<Type, object>();


            var unityContainer = new UnityContainer();

            _client = new ClientApp(new UnityServiceProvider(unityContainer),
                new UnityServiceCollection(unityContainer));
        }

        private async Task StartEventStoreProjections()
        {
            var connection = await GetEventStoreConnection();
            await connection.ProjectionsManager.EnableAll();
        }
        private async Task StartEventStoreInDocker()
        {
            Uri uri = new Uri("npipe://./pipe/docker_engine");

            DockerClient client = new DockerClientConfiguration(uri, defaultTimeout:TimeSpan.FromSeconds(5))
                .CreateClient();
            
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters()
            {
                All = true, Limit = 10000
            });
            var container = containers.FirstOrDefault(x => x.Names.Any(n => n == "/eventstore-mem"));
            if (container == null)
            {
                throw new NotImplementedException("You need to setup container for testing named eventstore-mem or write some code here to make it happen.");
            } 
            else
            {
                var data = await client.Containers.InspectContainerAsync(container.ID);
                if (data.State.Running)
                {
                    await client.Containers.RestartContainerAsync(data.ID, new ContainerRestartParameters());
                }
                else
                    await client.Containers.StartContainerAsync(data.ID, new ContainerStartParameters());
                // Just make sure EventStore runs.
                await Task.Delay(1000);
            }
           
        }
        private async Task StartWebServer()
        {
            this._host = WebHost
                .CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .UseUrls("http://localhost:5000")
                .UseStartup<Startup>()
                .Build();

            await this._host.StartAsync();
        }
        
        private IAggregateSchemaRegister _aggregateSchemaRegister;

        private IAggregateSchemaRegister GetAggregateSchema()
        {
            if (_aggregateSchemaRegister == null)
            {
                _aggregateSchemaRegister = _host.Services.GetService<IAggregateSchemaRegister>();
            }

            return _aggregateSchemaRegister;
        }

        

        public ISpecificationExecutor Init(IProjectionSchemaRegister projectionSchemaRegister)
        {
            foreach (var i in projectionSchemaRegister)
            {
                // hymmm 
            }

            return this;
        }

        public ISpecificationExecutor Init(IQuerySchemaRegister querySchemaRegister)
        {
            foreach (var i in querySchemaRegister)
            {
                _client.ServiceCollection.AddTransient(
                    typeof(IQueryHandler<,,>).MakeGenericType(i.Type, i.ModelType, i.ResultType), i.QueryHandlerType);

                foreach (var p in i.StreamPartitioners)
                    _client.ServiceCollection.AddTransient(typeof(IQueryPartitioner<>).MakeGenericType(i.Type), p);
            }

            return this;
        }

        public ISpecificationExecutor Init(IAggregateSchemaRegister aggregateSchemaRegister)
        {
            return this;
        }

        public async IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents()
        {
            await Task.Delay(200);
            _currentMode = ExecutorMode.Then;
            foreach (var i in _frames.Peek().Events)
                yield return i;
        }
        public async Task<(Guid, IEvent)> FindLestEvent(Type eventType)
        {
            await Task.Delay(200);
            _currentMode = ExecutorMode.Then;
            foreach (var frame in _frames.ToArray().Reverse())
            foreach (var ev in frame.Events)
            {
                if (ev.Item2.GetType() == eventType)
                    return ev;
            }

            return (Guid.Empty, null);
        }

        private bool TryGetEvent(ResolvedEvent e, out (Guid, IEvent) ev)
        {
            if (e.Event.ContentType == "application/json" && !e.Event.EventStreamId.StartsWith("$"))
            {
                Tuple<Guid, IEvent> result = null;
                try
                {
                    string metaContent = Encoding.UTF8.GetString(e.Event.Metadata);
                    EventMetadata m = JsonConvert.DeserializeObject<EventMetadata>(metaContent);

                    if (m != null && m.AggregateId != Guid.Empty)
                    {
                        var eventType = GetAggregateSchema()
                            .FindEventTypeByName(m.AggregateType, e.Event.EventType);
                        if (eventType != null)
                        {
                            var eventContent = Encoding.UTF8.GetString(e.Event.Data);
                            var eventData = (IEvent)JsonConvert.DeserializeObject(eventContent, eventType);
                            ev = (m.AggregateId, eventData);
                            return true;
                        }
                    }
                }
                catch { /*we dont care here*/ }
            }

            ev = (Guid.Empty, null);
            return false;
        }
        
        public async Task ExecuteCommand(IClientCommandSchema metadata, 
            Guid aggregateId, 
            ICommand cmd)
        {
            await CheckInitialized();
            
            _currentMode = ExecutorMode.When;
            await _executor.ExecuteCommand(metadata, aggregateId, cmd);
        }

        public async Task AppendFact(Guid aggregateId, IEvent ev)
        {
            await CheckInitialized();
            await Task.Delay(200);
            _currentMode = ExecutorMode.Given;
            var metadata = GetAggregateSchema().FindAggregateByEvent(ev.GetType());

            var mth = typeof(AppSpecificationExecutor).GetMethod(nameof(AppendSpecificFact),
                BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)mth.MakeGenericMethod(metadata.Type, ev.GetType())
                .Invoke(this, new object[] {aggregateId, ev});
            
        }

        private List<ILiveResult> _liveQueries;
        public async Task<ILiveResult> ExecuteQuery(IQuery query)
        {
            var args = query.GetType()
                .FindOpenInterfaces(typeof(IQuery<,>))
                .Single()
                .GetGenericArguments();

            var task = (Task<ILiveResult>)typeof(AppSpecificationExecutor)
                .GetMethod(nameof(ExecuteQuery),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .MakeGenericMethod(new[] { query.GetType(), args[1], args[0] })
                .Invoke(this, new[] { query });


            ILiveResult r = await task;
            _liveQueries.Add(r);
            return r;
        }

        private Dictionary<Type, object> _queryEngines;
        

        private IQueryEngine<TModel> Engine<TModel>()
        where TModel : IModel
        {
            if(!_queryEngines.TryGetValue(typeof(TModel), out object engine))
            {
                engine = ActivatorUtilities.CreateInstance<QueryEngine<TModel>>(_client.ServiceProvider);
                _queryEngines.Add(typeof(TModel), engine);
            }
            return (IQueryEngine < TModel > )engine;
        }

        private async Task<ILiveResult> ExecuteQuery<TQuery, TResult, TModel>(TQuery query)
            where TModel : IModel
            where TQuery : IQuery<TModel, TResult>
            where TResult : class
        {
            var engine = Engine<TModel>();
            var result = await engine.Execute<TQuery, TResult>(query);

            return result;
        }

        public IEnumerable<ILiveResult> GetQueryResults()
        {
            return _liveQueries;
        }

        
       

        private IEventStoreFacade _connection;
        private async Task<IEventStoreFacade> GetEventStoreConnection()
        {
            if (_connection == null)
            {
                
                _connection = new EventStoreFacade("https://localhost:2113", "tcp://localhost:1113","admin","changeit");
                //_connection = new HttpEventStoreClient("tcp://localhost:1113", "admin", "changeit");
                await _connection.SubscribeToAllAsync(OnEventAppended, true);
            }

            return _connection;
        }

        private Task OnEventAppended(IStreamSubscription arg1, ResolvedEvent ev, CancellationToken arg3)
        {
            if (ev.Link != null || ev.OriginalEvent.EventStreamId.StartsWith("$"))
            {
                return Task.CompletedTask;
            }

            if (TryGetEvent(ev, out (Guid, IEvent) e))
            {
                
                var stackFrame = _frames.Peek();
                if (_currentMode != stackFrame.Mode)
                    _frames.Push(new StackFrame(_currentMode));
                stackFrame.Events.Push(e);
                Debug.WriteLine($"Saving events for frame: {e.Item2.Id}");
            }

            return Task.CompletedTask;
        }

        private Stack<StackFrame> _frames;
        

        private async Task AppendSpecificFact<TAggregate, TEvent>(
            Guid aggregateId, 
            TEvent ev)
        where TEvent:IEvent
        where TAggregate:IAggregate
        {
            await CheckInitialized();
            var schema = _host.Services.GetService<IAggregateSchema<TAggregate>>();

            AggregateEventStream<TAggregate> stream = new AggregateEventStream<TAggregate>(
                await GetEventStoreConnection(), 
                new EventConverter(), 
                new EventDataFactory(), new EventMetadataFactory<TAggregate>(), schema, Logger.None);

            await stream.Append(aggregateId, Guid.NewGuid(), ev);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            Task.WhenAll(_host.StopAsync());
            _host.Dispose();

            if (node != null)
                Task.WhenAll(node.StopAsync());
        }
    }
    class CommandSpecExecutor
    {
        readonly IServiceDiscovery srvDisco;
        readonly IHttpClient client;

        public CommandSpecExecutor()
        {
           srvDisco = new InternalDiscoService();
           client = new HttpAppClient(new ClientSession(), Logger.None);
        }

        public async Task ExecuteCommand(IClientCommandSchema metadata,
            Guid aggregateId,
            ICommand cmd)
        {
            var url = srvDisco.Discover(cmd.GetType());
            await client.PostAsync(url, aggregateId, cmd);
        }
    }
}