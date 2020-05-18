using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using EventDrivenThinking.Utils;


namespace EventDrivenThinking.Example.Model.ReadModels.Hotel
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
        public int Floor { get; set; }
    }

    public class RoomAvailabilityStreamPartitioner : IProjectionStreamPartitioner<RoomAvailabilityProjection>
    {
        public Guid[] CalculatePartitions(EventMetadata m, IEvent ev)
        {
            List<Guid> partitions = new List<Guid>(1);
            switch (ev)
            {
                case RoomBooked rb:
                    {
                        int nr = Int32.Parse(rb.Number);
                        int roomOnFloor = nr % 100;
                        int floor = (nr - roomOnFloor) / 100;
                        partitions.Add(floor.ToString().ToGuid());
                    }
                    break;
                case RoomAdded ra:
                    {
                        int nr = Int32.Parse(ra.Number);
                        int roomOnFloor = nr % 100;
                        int floor = (nr - roomOnFloor) / 100;
                        partitions.Add(floor.ToString().ToGuid());
                    }
                    break;
            }

            return partitions.ToArray();
        }
    }

    public class GetAvailabilityForRoomQueryPartitioner : IQueryPartitioner<GetAvailabilityForRoomQuery>
    {
        public Guid CalculatePartition( GetAvailabilityForRoomQuery query)
        {
            string partition = query.Floor.ToString();
            return partition.ToGuid();
        }
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