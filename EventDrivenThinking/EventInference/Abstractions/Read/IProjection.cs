using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IProjection
    {
        IModel Model { get; }
        Task Execute(IEnumerable<(EventMetadata, IEvent)> events);
    }
    public interface IProjection<out TModel> : IProjection where TModel : IModel
    {
        new TModel Model { get; }
    }
}