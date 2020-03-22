using System;

namespace EventDrivenThinking.Example.Model.ReadModels.Hotel
{
    public class Reservation
    {
        public virtual DateTime From { get; set; }
        public virtual DateTime To { get; set; }
    }
}