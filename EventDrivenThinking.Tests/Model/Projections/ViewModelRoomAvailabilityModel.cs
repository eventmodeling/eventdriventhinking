using System.Collections.Generic;
using EventDrivenThinking.Ui;

namespace EventDrivenUi.Tests.Model.Projections
{
    public class ViewModelRoomAvailabilityModel : IRoomAvailabilityModel
    {
        public ViewModelRoomAvailabilityModel()
        {
            Rooms = new ViewModelCollection<Room>(new List<Room>());
        }
        public ICollection<Room> Rooms { get; }
        public T Create<T>()
        {
            return ViewModelFactory<T>.Create();
        }
    }
}