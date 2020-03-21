using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.QueryProcessing;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Core;

#pragma warning disable 1998
namespace EventDrivenThinking.Tests.Common
{
    enum ExecutorMode
    {
        Given, When, Then
    }
    public class AppSpecificationExecutor : ISpecificationExecutor
    {
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
                //StartEventStore();
                await StartEventStoreInDocker();
                await StartWebServer();
                await StartEventStoreProjections();
            }
        }

        

        private ClusterVNode node;
        private IWebHost _host;
        private CommandSpecExecutor _executor;

        public AppSpecificationExecutor()
        {
            _executor = new CommandSpecExecutor();
            _frames = new Stack<StackFrame>();
            _frames.Push(new StackFrame(ExecutorMode.Given));
        }

        private async Task StartEventStoreProjections()
        {
            var httpMessageHandler = new HttpClientHandler();
            ProjectionsManager pm = new ProjectionsManager(new ConsoleLogger(),
                new DnsEndPoint("localhost", 2113), TimeSpan.FromSeconds(5), httpMessageHandler);
            var userCredentials = new UserCredentials("admin", "changeit");
            var projections = await pm.ListAllAsync(userCredentials);
            var toEnable = projections.Select(p => pm.EnableAsync(p.Name, userCredentials)).ToArray();
            Task.WaitAll(toEnable);
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
                    await client.Containers.StopContainerAsync(data.ID, new ContainerStopParameters());
                }

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
        internal void StartEventStore()
        {
            var nodeBuilder = EmbeddedVNodeBuilder.AsSingleNode()
                .OnDefaultEndpoints()
                
                .RunInMemory();
            this.node = nodeBuilder.Build();
            node.Start();
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
            return this;
        }

        public ISpecificationExecutor Init(IQuerySchemaRegister querySchemaRegister)
        {
            return this;
        }

        public ISpecificationExecutor Init(IAggregateSchemaRegister aggregateSchemaRegister)
        {
            return this;
        }

        public async IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents()
        {
            await Task.Delay(1000);
            _currentMode = ExecutorMode.Then;
            foreach (var i in _frames.Pop().Events)
                yield return i;
        }

        private bool TryGetEvent(ResolvedEvent e, out (Guid, IEvent) ev)
        {
            if (e.IsResolved && e.Event.IsJson)
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
            await Task.Delay(1000);
            _currentMode = ExecutorMode.When;
            await _executor.ExecuteCommand(metadata, aggregateId, cmd);
        }

        public async Task AppendFact(Guid aggregateId, IEvent ev)
        {
            await CheckInitialized();
            await Task.Delay(1000);
            _currentMode = ExecutorMode.Given;
            var metadata = GetAggregateSchema().FindAggregateByEvent(ev.GetType());

            var mth = typeof(AppSpecificationExecutor).GetMethod(nameof(AppendSpecificFact),
                BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)mth.MakeGenericMethod(metadata.Type, ev.GetType())
                .Invoke(this, new object[] {aggregateId, ev});
            
        }

        public Task<IQueryResult> ExecuteQuery(IQuery query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IQueryResult> GetQueryResults()
        {
            throw new NotImplementedException();
        }

        public Task<TResult> ExecuteQuery<TQuery, TResult, TModel>(TQuery query) where TQuery : IQuery<TModel, TResult> where TModel : IModel
        {
            throw new NotImplementedException();
        }

       

        private IEventStoreConnection _connection;
        private async Task<IEventStoreConnection> GetEventStoreConnection()
        {
            if (_connection == null)
            {
                _connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
                await _connection.ConnectAsync();
                await _connection.SubscribeToAllAsync(true, OnEventAppended, null, null);
            }

            return _connection;
        }

        private Stack<StackFrame> _frames;
        private async Task OnEventAppended(EventStoreSubscription subscription, ResolvedEvent ev)
        {
            if (TryGetEvent(ev, out (Guid, IEvent) e))
            {
                var stackFrame = _frames.Peek();
                if(_currentMode != stackFrame.Mode)
                    _frames.Push(new StackFrame(_currentMode));
                stackFrame.Events.Push(e);
            }
        }

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
                new EventDataFactory(), new EventMetadataFactory<TAggregate>(), schema, Logger.None);

            await stream.Append(aggregateId, ExpectedVersion.Any, Guid.NewGuid(), ev);
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