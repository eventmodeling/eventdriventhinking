using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;

namespace EventDrivenThinking.Example.Model.Hotel
{
    public class HotelAggregate : Aggregate<HotelAggregate.State>
    {
        public override Guid Id
        {
            get => base._state.Id;
            set => _state.Id = (Guid) value;
        } 
        public class State
        {
            private List<string> _availableRooms;
            public Guid Id { get; set; }
            public IList<string> AvailableRooms => _availableRooms;
            public bool IsRoomAvailable()
            {
                return _availableRooms.Any();
            }
            
            public State()
            {
                _availableRooms = new List<string>();
            }
        }

        internal State Value => _state;

        private static RoomClosed When(State st, CloseRoom cmd)
        {
            return new RoomClosed();
        }

        private static IEnumerable<IEvent> When(State st, BookRoom cmd)
        {
            if (st.IsRoomAvailable())
                yield return new RoomBooked()
                {
                    Start = cmd.Start, End = cmd.End,
                    Number = st.AvailableRooms.First()
                };
            else throw new Exception();
        }

        private static State Given(State st, RoomAdded ev)
        {
            st.AvailableRooms.Add(ev.Number);
            return st;
        }
        private static State Given(State st, RoomBooked ev)
        {
            st.AvailableRooms.Remove(ev.Number);
            return st;
        }

    }
}