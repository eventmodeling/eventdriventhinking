using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.Integrations.SignalR
{
    public interface IEventStoreHubInitializer
    {
        void Init();
    }
    public class EventStoreHubInitializer : IEventStoreHubInitializer
    {
        private readonly IEventStoreConnection _connection;
        private readonly IHubContext<EventStoreHub> _hubConnection;
        private readonly ILogger _logger;
        private readonly IProjectionSchemaRegister _projectionSchema;

        public EventStoreHubInitializer(IEventStoreConnection connection, 
            IHubContext<EventStoreHub> hubConnection, ILogger logger,
            IProjectionSchemaRegister projectionSchema)
        {
            this._connection = connection;
            this._hubConnection = hubConnection;
            _logger = logger;
            this._projectionSchema = projectionSchema;
            
        }

        public void Init()
        {
            _connection.BindToSignalHub(_projectionSchema, _hubConnection, _logger);
        }
    }
}