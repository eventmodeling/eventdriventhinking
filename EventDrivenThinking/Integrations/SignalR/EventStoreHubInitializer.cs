using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventStore;
using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace EventDrivenThinking.Integrations.SignalR
{
    public interface IEventStoreHubInitializer
    {
        Task Init();
    }
    public class EventStoreHubInitializer : IEventStoreHubInitializer
    {
        private readonly IEventStoreFacade _connection;
        private readonly IHubContext<EventStoreHub> _hubConnection;
        private readonly ILogger _logger;
        private readonly IEventConverter _eventConverter;
        private readonly IProjectionSchemaRegister _projectionSchema;

        public EventStoreHubInitializer(IEventStoreFacade connection, 
            IHubContext<EventStoreHub> hubConnection, ILogger logger,
            IEventConverter eventConveter,
            IProjectionSchemaRegister projectionSchema)
        {
            this._connection = connection;
            this._hubConnection = hubConnection;
            this._logger = logger;
            this._eventConverter = eventConveter;
            this._projectionSchema = projectionSchema;
            
        }

        public Task Init()
        {
            return _connection.BindToSignalHub(_eventConverter, 
                _projectionSchema, 
                _hubConnection, 
                _logger);
        }
    }
}