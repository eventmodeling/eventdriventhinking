using System;
using System.Collections.Generic;
using EventDrivenThinking.Ui;
using EventDrivenUi.Tests.Model.Projections;

namespace EventDrivenUi.Tests.Model.Hotel
{
    // The responsibility of view model is to create commands
    // and coordinate non-functional configuration of views. 
    // Data in views can be updated automatically as projections are shared.

    public class HotelViewModel : ViewModelBase<HotelViewModel>
    {
        private readonly IRoomAvailabilityModel _model;
        
        public ICollection<Room> Rooms => _model.Rooms;

        public HotelViewModel(Func<IRoomAvailabilityModel> model, Guid id) :  base(id)
        {
            _model = model();
        }

        private (Guid, BookRoom) CreateBookRoomCommand()
        {
            return (Id, new BookRoom());
        }
        // List of Rooms

    }

    
}