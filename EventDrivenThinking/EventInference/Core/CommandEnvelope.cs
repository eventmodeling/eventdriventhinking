using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Core
{
    public class CommandEnvelope<TKey, TCommand> : CommandEnvelope
        where TCommand : ICommand
    {
        public CommandEnvelope(TKey id, TCommand command) : base(id, command)
        {
            Id = id;
            Command = command;
        }

        public new TKey Id { get; }
        public new TCommand Command { get; }

        public static implicit operator CommandEnvelope<TKey, TCommand>((TKey, TCommand) args)
        {
            return new CommandEnvelope<TKey, TCommand>(args.Item1, args.Item2);
        }
    }
}