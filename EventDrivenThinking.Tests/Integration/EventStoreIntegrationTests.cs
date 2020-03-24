using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using EventDrivenThinking.Example.Model.ReadModels.Hotel;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenThinking.Tests.Common;
using EventDrivenThinking.Utils;
using EventStore.Client;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Utils;
using FluentAssertions;
using Io.Cucumber.Messages;
using Newtonsoft.Json;
using Polly;
using Serilog.Core;
using Unity;
using Xunit;
using Xunit.Abstractions;
using EventData = EventStore.Client.EventData;
using Position = EventStore.ClientAPI.Position;
using UserCredentials = EventStore.ClientAPI.SystemData.UserCredentials;


namespace EventDrivenThinking.Tests.Integration
{
    
    public class EventStoreIntegrationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public EventStoreIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task CanConnect()
        {
            Type t = typeof(ProtoBuf.BclHelpers);
            ConnectionSettings settings = ConnectionSettings.Create().UseSslConnection(false)
                //.UseDebugLogger()
                .EnableVerboseLogging()
                .KeepRetrying()
                //.SetHeartbeatTimeout(TimeSpan.FromSeconds(30))
                .SetDefaultUserCredentials(new UserCredentials("admin","changeit"))
                .Build();
            
            var connection = EventStoreConnection.Create(settings, new Uri("tcp://localhost:1113"));
            //var connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));

            await connection.ConnectAsync();
            var result = await connection.ReadAllEventsForwardAsync(Position.Start, 10, true);

        }

        [Fact]
        public async Task LinksCanBeCreated()
        {
            //await EventStoreServer.Instance.Start();

            var connection = await Connect();

            string projectionStreamName =$"Projection-{Guid.NewGuid()}" ;
            string streamName = $"Foo-{Guid.NewGuid().ToString()}";
            RoomAdded eventObj = null;

            for (int i = 0; i < 10; i++)
            {
                eventObj = new RoomAdded() { Number = "101" };

                var metadata = new EventMetadata(Guid.NewGuid(), typeof(HotelAggregate), Guid.NewGuid(), 0);
                var eventData = new EventData(Uuid.NewUuid(), "RoomAdded",  eventObj.ToJsonBytes(), metadata.ToJsonBytes());
                var result = await connection.AppendToStreamAsync(streamName, AnyStreamRevision.Any, 
                    new []{eventData});

                var linkData = new EventData(Uuid.NewUuid(), "$>", Encoding.UTF8.GetBytes($"{i}@{streamName}"), null);

                var projectionStream = await connection.AppendToStreamAsync(projectionStreamName, AnyStreamRevision.Any, new []{ linkData});
                
            }

            await foreach (var e in connection.ReadStreamAsync(Direction.Backwards, projectionStreamName,
                StreamRevision.Start, 20, resolveLinkTos:true))
            {
                var data = e.Event.Data.FromJsonBytes<RoomAdded>();
                data.Number.Should().Be(eventObj.Number);
                break;
            }
        }

        [Fact]
        public async Task AggregateShouldWorkWithEventStore()
        {
            IAggregateSchemaRegister schemaRegister = new AggregateSchemaRegister();
            schemaRegister.Discover(typeof(EventStoreIntegrationTests).Assembly);

            var hotelSchema = new AggregateSchema<HotelAggregate>(schemaRegister.Get(typeof(HotelAggregate)));
            var stream = GetStream(await Connect(), hotelSchema);

            var aggregateId = Guid.NewGuid();
            await stream.Append( aggregateId,-2, Guid.NewGuid(), new RoomAdded() {Number = "101"});

            stream = GetStream(await Connect(), hotelSchema);

            var items = stream.Get(aggregateId);
            HotelAggregate aggregate = new HotelAggregate();
            await aggregate.RehydrateAsync(items);

           // aggregate.Value.AvailableRooms.Should().HaveCount(1);
        }

        [Fact]
        public async Task ProjectionsShouldWorkWithEventStore()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetry(100, x => TimeSpan.FromMilliseconds(50));

            await Cleanup();

            var aggregateId = Guid.NewGuid();

            await AppendEvent(aggregateId);

            IProjectionSchemaRegister schema = new ProjectionSchemaRegister();
            schema.Discover(typeof(EventStoreIntegrationTests).Assembly);

            var connection = await Connect();
            UnityContainer uc = new UnityContainer();
            uc.RegisterSingleton<IRoomAvailabilityModel, RoomAvailabilityModel>();

            IServiceProvider serviceprovider = new UnityServiceProvider(uc);
            throw new NotImplementedException();
            //connection.ConfigureWithEventBus(schema.Events,serviceprovider.GetService<IEventHandlerDispatcher>(), Logger.None);

            var model = uc.Resolve<IRoomAvailabilityModel>();
            policy.Execute(() =>  model.Rooms.Should().HaveCount(1));

            await AppendEvent(Guid.NewGuid());
            Thread.Sleep(2000);
            policy.Execute(() => model.Rooms.Should().HaveCount(2));
        }

        private async Task Cleanup()
        {
            var connection = await Connect();
            //await connection.DeleteStreamAsync($"$et-{nameof(RoomBooked)}",ExpectedVersion.Any);
            //await connection.DeleteStreamAsync($"$et-{nameof(RoomAdded)}", ExpectedVersion.Any);
        }

        private static async Task AppendEvent(Guid aggregateId)
        {
            IAggregateSchemaRegister schemaRegister = new AggregateSchemaRegister();
            schemaRegister.Discover(typeof(EventStoreIntegrationTests).Assembly);

            var stream = GetStream(await Connect(), new AggregateSchema<HotelAggregate>(schemaRegister.Get(typeof(HotelAggregate))));
            //await stream.Append(aggregateId, ExpectedVersion.Any,Guid.NewGuid(),new RoomAdded() {Number = "101"});
            throw new NotImplementedException();
        }

        private static AggregateEventStream<HotelAggregate> GetStream(IEventStoreFacade connection, 
            IAggregateSchema<HotelAggregate> schema)
        {
            AggregateEventStream<HotelAggregate> stream = new AggregateEventStream<HotelAggregate>(connection,
                new EventDataFactory(),new EventMetadataFactory<HotelAggregate>(), schema, Logger.None);
            return stream;
        }

        private static async Task<IEventStoreFacade> Connect()
        {
            var client = new EventStoreFacade("https://localhost:2113", "tcp://localhost:1113", "admin", "changeit");


            return client;
            //IHttpEventStoreClient connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            //await connection.ConnectAsync();
            //return connection;
        }
    }
}