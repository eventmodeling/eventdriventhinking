using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public interface IEventHandler<in TEvent>
        where TEvent:IEvent
    {
        Task Execute(EventMetadata m, TEvent ev);
    }
}