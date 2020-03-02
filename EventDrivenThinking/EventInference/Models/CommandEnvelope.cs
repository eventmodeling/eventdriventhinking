using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Models
{
    public class CommandEnvelope
    {
        public CommandEnvelope(object id, ICommand command)
        {
            Id = id;
            Command = command;
        }

        public object Id { get; protected set; }
        public ICommand Command { get; protected set; }
    }
}