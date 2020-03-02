using System;
using System.Collections;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.Ui
{
    public interface IUiEventBus
    {
        IEnumerable PublishedEvents { get; }
        IEventPublisher<T> GetEvent<T>();
        void InvokeCommand<T>(Guid id, T command) where T : ICommand;
        
    }
}