using System;

namespace EventDrivenThinking.EventInference.Abstractions.Write
{
    public interface ICommand
    {
        public Guid Id { get; }
    }
}