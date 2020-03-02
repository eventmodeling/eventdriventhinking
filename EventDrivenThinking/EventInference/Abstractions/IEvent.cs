using System;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public interface IEvent
    {
        Guid Id { get; }
    }
}