using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Core
{
    public class CommandEnvelope<TKey, TCommand> : CommandEnvelope<TKey>
        where TCommand : ICommand
    {
        public CommandEnvelope(TKey id, TCommand command) : base(id, command)
        {
        }

        public new TCommand Command => (TCommand) base.Command;

        public static implicit operator CommandEnvelope<TKey, TCommand>((TKey, TCommand) args)
        {
            return new CommandEnvelope<TKey, TCommand>(args.Item1, args.Item2);
        }
    }
    public class CommandEnvelope<TKey> : CommandEnvelope
    {
        public new TKey Id => (TKey) base.Id;

        public CommandEnvelope(TKey id, ICommand command) : base(id, command)
        {
        }
    }
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