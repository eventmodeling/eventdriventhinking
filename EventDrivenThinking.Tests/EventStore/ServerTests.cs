using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Xunit;

namespace EventDrivenThinking.Tests.EventStore
{
    public class ServerTests
    {
        [Fact]
        public async Task CanConnect()
        {
            IEventStoreConnection connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            await connection.ConnectAsync();
        }
    }
}
