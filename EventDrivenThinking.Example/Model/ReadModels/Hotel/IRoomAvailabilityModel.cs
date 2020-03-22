using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.Example.Model.ReadModels.Hotel
{
    public interface IRoomAvailabilityModel : IModel
    {
        ICollection<Room> Rooms { get; }
        T Create<T>();
    }
}