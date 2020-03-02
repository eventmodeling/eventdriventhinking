using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Abstractions.Write
{
    public interface IProcessor
    {
        IEnumerable<(Guid, ICommand)> When<TEvent>(EventMetadata m, TEvent ev)
            where TEvent : IEvent;
    }

    public class NotFoundException : Exception
    {

    }
}