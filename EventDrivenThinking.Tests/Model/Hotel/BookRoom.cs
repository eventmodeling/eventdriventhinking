using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenUi.Tests.Model.Hotel
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