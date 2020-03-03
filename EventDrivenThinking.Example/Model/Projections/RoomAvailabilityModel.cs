using System;
using System.Collections.Generic;

namespace EventDrivenThinking.Example.Model.Projections
{
    public class RoomAvailabilityModel : IRoomAvailabilityModel
    {
        public RoomAvailabilityModel()
        {
            Rooms = new List<Room>();
        }

        public virtual ICollection<Room> Rooms { get; }

        public virtual T Create<T>()
        {
            return Activator.CreateInstance<T>();
        }
    }
}