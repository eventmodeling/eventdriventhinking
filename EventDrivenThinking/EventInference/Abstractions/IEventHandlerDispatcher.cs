using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Abstractions
{
    /// <summary>
    /// This is invoked on the edge of the app:
    /// On server it is used to call eventHandler
    /// In in-memory scenario it is called by event-aggregator.
    /// It create new scopes.
    /// </summary>
    public interface IEventHandlerDispatcher
    {
        Task Dispatch<TEvent>(EventMetadata m, TEvent ev)
            where TEvent : IEvent;
    }
}