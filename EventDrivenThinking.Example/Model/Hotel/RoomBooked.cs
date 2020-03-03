using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.Example.Model.Hotel
{
    public class CloseRoom : ICommand
    {
        public Guid Id { get; set; }
    }
    public class RoomClosed : IEvent
    {
        public Guid Id { get; set; }
    }
    public class RoomBooked : IEvent
    {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public RoomBooked()
        {
            Id = Guid.NewGuid();
        }
    }

    public class RoomAdded : IEvent
    {
        public RoomAdded()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public string Number { get; set; }
    }
    
}