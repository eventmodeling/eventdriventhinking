using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Write;
using Unity;

namespace EventDrivenThinking.EventInference.Abstractions
{
    /// <summary>
    /// Calls appropriate CommandHandler
    /// Is responsible for Scope
    /// </summary>
    public interface ICommandDispatcher
    {
        Task Dispatch<TCommand>(Guid id, TCommand cmd)
            where TCommand : ICommand;
    }

    public interface ICommandInvoker<in TCommand>
        where TCommand : ICommand
    {
        Task Invoke(Guid id, TCommand cmd);
    }
}