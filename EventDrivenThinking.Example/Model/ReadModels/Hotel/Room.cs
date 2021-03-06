﻿using System.Collections.Generic;

namespace EventDrivenThinking.Example.Model.ReadModels.Hotel
{
    public class Room
    {
        public virtual string Number { get; set; }
        public virtual IList<Reservation> Reservations { get; }

        public Room()
        {
            Reservations = new List<Reservation>();
        }
    }
}