using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;

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
    public class GetAvailabilityForRoomQuery : IQuery<IRoomAvailabilityModel, RoomAvailabilityResults>
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class RoomAvailabilityResults
    {
        public virtual ICollection<string> AvailableRooms { get; set; }
        public bool Found => AvailableRooms?.Any() ?? false;
    }

    [QueryHandlerMarkup]
    public class RoomAvailabilityQueryHandler
    {
        public virtual RoomAvailabilityResults Execute(IRoomAvailabilityModel model, GetAvailabilityForRoomQuery query)
        {
            List<string> rooms = new List<string>();

            foreach (var i in model.Rooms)
            {
                var overlapping = i.Reservations.FirstOrDefault(x => (x.From <= query.Start && query.Start < x.To) ||
                          (x.From < query.End && query.End <= x.To) ||
                          (query.Start < x.From && x.To < query.End));

                if(overlapping == null)
                    rooms.Add(i.Number);
            }

            return new RoomAvailabilityResults() {AvailableRooms = rooms};
        }
    }
}