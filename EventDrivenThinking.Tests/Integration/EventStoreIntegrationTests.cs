﻿using System;
using System.ComponentModel;
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
using EventDrivenThinking.Utils;
using EventStore.ClientAPI;
using EventStore.Common.Utils;
using FluentAssertions;
using Io.Cucumber.Messages;
using Newtonsoft.Json;
using Polly;
using Serilog.Core;
using Unity;
using Xunit;
using Xunit.Abstractions;

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
        public async Task LinksCanBeCreated()
        {
            var connection = await Connect();

            
            string streamName = $"Foo-{Guid.NewGuid().ToString()}";
            RoomAdded eventObj = null;

            for (int i = 0; i < 10; i++)
            {
                eventObj = new RoomAdded() { Number = "101" };

                var metadata = new EventMetadata(Guid.NewGuid(), typeof(HotelAggregate), Guid.NewGuid(), 0);
                var result = await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any,
                    new EventData(Guid.NewGuid(), "RoomAdded", true, eventObj.ToJsonBytes(), metadata.ToJsonBytes()));

                var linkData = new EventData(Guid.NewGuid(), "$>", false, Encoding.UTF8.GetBytes($"{i}@{streamName}"), null);

                var projectionStream =
                    await connection.AppendToStreamAsync("projection", ExpectedVersion.Any, linkData);

             
            }
            var readProjection = await connection.ReadStreamEventsForwardAsync("projection", 0, 20, true);
            var data = readProjection.Events.Last().Event.Data.FromJsonBytes<RoomAdded>();
            data.Should().BeEquivalentTo(eventObj);
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