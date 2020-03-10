using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;
using Serilog;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public class ProcessorEventHandler<TProcessor, TEvent> : IEventHandler<TEvent>
        where TProcessor:IProcessor
        where TEvent : IEvent

    {
        private readonly TProcessor _processor;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger _logger;

        public ProcessorEventHandler(TProcessor processor, ICommandDispatcher commandDispatcher, ILogger logger)
        {
            _processor = processor;
            _commandDispatcher = commandDispatcher;
            _logger = logger;
        }

        public async Task Execute(EventMetadata m, TEvent ev)
        {
            // TODO:
            // Handle exceptions. Invoking a command can cause exception.
            // An exception should be handled TProcessor, by another method such as HandleError. 
            // We want TProcessor to be divided into 2: part where commands are prepared
            // from how errors are handled.
            _logger.Information("{processorName} received from {aggregateId} an {eventName} {eventId}", typeof(TProcessor).Name, m.AggregateId, typeof(TEvent).Name, ev.Id);

            var commands = await _processor.When(m, ev);
            Task[] tasks = new Task[commands.Length];
            for (var index = 0; index < commands.Length; index++)
            {
                var cmdEnv = commands[index];
                tasks[index] = _commandDispatcher.Dispatch(cmdEnv.Id, cmdEnv.Command);
            }

            await Task.WhenAll(tasks);
        }
    }
}