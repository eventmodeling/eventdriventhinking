using System;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.Example.Model.Hotel
{
    
    public class BookRoom : ICommand
    {
        public BookRoom()
        {
            Id = Guid.NewGuid();
            Start = DateTime.Now;
            End = Start.AddDays(5);
        }
        public Guid Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}