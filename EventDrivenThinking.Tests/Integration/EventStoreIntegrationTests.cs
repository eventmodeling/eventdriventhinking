using System;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Hotel;
using EventDrivenThinking.Example.Model.Projections;
using EventDrivenThinking.Integrations.Unity;
using EventStore.ClientAPI;
using FluentAssertions;
using Polly;
using Serilog.Core;
using Unity;
using Xunit;

namespace EventDrivenThinking.Tests.Integration
{
    
    public class EventStoreIntegrationTests
    {
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
            await connection.DeleteStreamAsync($"$et-{nameof(RoomBooked)}",ExpectedVersion.Any);
            await connection.DeleteStreamAsync($"$et-{nameof(RoomAdded)}", ExpectedVersion.Any);
        }

        private static async Task AppendEvent(Guid aggregateId)
        {
            IAggregateSchemaRegister schemaRegister = new AggregateSchemaRegister();
            schemaRegister.Discover(typeof(EventStoreIntegrationTests).Assembly);

            var stream = GetStream(await Connect(), new AggregateSchema<HotelAggregate>(schemaRegister.Get(typeof(HotelAggregate))));
            await stream.Append(aggregateId, ExpectedVersion.Any,Guid.NewGuid(),new RoomAdded() {Number = "101"});
        }

        private static AggregateEventStream<HotelAggregate> GetStream(IEventStoreConnection connection, 
            IAggregateSchema<HotelAggregate> schema)
        {
            AggregateEventStream<HotelAggregate> stream = new AggregateEventStream<HotelAggregate>(connection,
                new EventDataFactory(),new EventMetadataFactory<HotelAggregate>(), schema, Logger.None);
            return stream;
        }

        private static async Task<IEventStoreConnection> Connect()
        {
            IEventStoreConnection connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            await connection.ConnectAsync();
            return connection;
        }
    }
}