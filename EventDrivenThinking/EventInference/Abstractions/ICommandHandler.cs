using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        Task When(Guid id, TCommand cmd);
    }
}