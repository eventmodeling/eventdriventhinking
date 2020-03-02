using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenUi.Tests.Model.Projections
{
    public interface IRoomAvailabilityModel : IModel
    {
        ICollection<Room> Rooms { get; }
        T Create<T>();
    }
}