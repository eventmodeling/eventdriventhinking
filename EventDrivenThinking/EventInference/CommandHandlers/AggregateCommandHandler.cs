using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.SessionManagement;
using EventDrivenThinking.Integrations.Carter;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.EventInference.CommandHandlers
{
    /// <summary>
    /// This class is used in dynamic code generation
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    public class AggregateCommandHandler<TAggregate, TCommand> : ICommandHandler<TCommand>
        where TAggregate : IAggregate, new()
        where TCommand:ICommand
    {
        private readonly IAggregateEventStream<TAggregate> _eventStream;
        private readonly ISessionContext _sessionContext;
        private readonly ILogger _logger;

        public AggregateCommandHandler(IAggregateEventStream<TAggregate> aggregateEventStream, ISessionContext sessionContext, ILogger logger)
        {
            _eventStream = aggregateEventStream;
            _sessionContext = sessionContext;
            _logger = logger;
        }


        public async Task When(Guid id, TCommand cmd)
        {
            _logger.Debug("Invoking a command {commandType} on an aggregate: {aggregateType}", cmd.GetType().Name, typeof(TAggregate).Name);
            var events = _eventStream.Get(id);
            var aggregate = new TAggregate { Id = id };

            await aggregate.RehydrateAsync(events);
            var version = aggregate.Version;

            var published = aggregate.Execute(cmd);

            var commitedEvents =
                version == 0 ?  await _eventStream.Append(aggregate.Id, cmd.Id, published) :
                                await _eventStream.Append(aggregate.Id, version-1, cmd.Id, published);

            // We return events to client - think about correlation-id solution?
            var current = _sessionContext.Current();
            if (current.IsValid)
            {
                _logger.Information("Sending events {eventsCount} on session {sessionId}.", commitedEvents.Length, current.Id);
                current.SendEvents(commitedEvents);
            }
        }
    }
}