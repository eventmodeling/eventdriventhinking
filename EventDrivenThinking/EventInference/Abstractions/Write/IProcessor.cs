using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Abstractions.Write
{
    public interface IProcessor
    {
        Task<CommandEnvelope<Guid>[]> When<TEvent>(EventMetadata m, TEvent ev)
            where TEvent : IEvent;
    }
    
    public class NotFoundException : Exception
    {

    }
}