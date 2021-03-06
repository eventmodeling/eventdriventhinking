﻿using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Example.Model.Domain.Hotel;

#pragma warning disable 1998
namespace EventDrivenThinking.Example.Model.ReadModels.Hotel
{
    public class RoomAvailabilityProjection : Projection<IRoomAvailabilityModel>
    {
        private static async Task Given(IRoomAvailabilityModel model,
            EventMetadata metadata, RoomBooked ev)
        {
            var room = model.Rooms.First(x => x.Number == ev.Number);
            
            var reservation = model.Create<Reservation>();
            reservation.To = ev.End;
            reservation.From = ev.Start;

            room.Reservations.Add(reservation);
        }
        private static async Task Given(IRoomAvailabilityModel model,
            EventMetadata metadata, RoomAdded ev)
        {
            var item = model.Create<Room>();
            item.Number = ev.Number;

            model.Rooms.Add(item);
        }


        public RoomAvailabilityProjection(IRoomAvailabilityModel model) : base(model)
        {
        }
    }
}