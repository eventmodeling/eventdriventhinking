using System;
using EventDrivenUi.Tests.Model.Hotel;
using EventDrivenUi.Tests.Model.Projections;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenUi.Tests
{
    public static class StartupServiceExtensions
    {
        public static void AddEventStoreTesting(this IServiceCollection services)
        {
            // Should be changed to scoped!
            services.AddSingleton<IRoomAvailabilityModel, RoomAvailabilityModel>();

            var connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            connection.ConnectAsync().GetAwaiter().GetResult();
            connection.DeleteStreamAsync($"$et-{nameof(RoomBooked)}", ExpectedVersion.Any).GetAwaiter().GetResult();
            connection.DeleteStreamAsync($"$et-{nameof(RoomAdded)}", ExpectedVersion.Any).GetAwaiter().GetResult();

            services.AddSingleton(typeof(IEventStoreConnection), connection);
        }
    }
}