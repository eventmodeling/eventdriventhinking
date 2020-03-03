using System;

namespace EventDrivenThinking.Example.Model.Projections
{
    public class Reservation
    {
        public virtual DateTime From { get; set; }
        public virtual DateTime To { get; set; }
    }
}