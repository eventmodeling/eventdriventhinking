using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.Example.Model.Projections
{
    public interface IRoomAvailabilityModel : IModel
    {
        ICollection<Room> Rooms { get; }
        T Create<T>();
    }
}