using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Example.Model.Hotel;
using EventDrivenThinking.Example.Model.Projections;
using FluentAssertions;
using Xunit;

namespace EventDrivenThinking.Tests
{
    public class RoomAvailabilityProjectionTests
    {
        [Fact]
        public async Task GivenWorksWithViewModel()
        {
            int collectionChanged = 0;
            var vm = new ViewModelRoomAvailabilityModel();
            ((INotifyCollectionChanged) vm.Rooms).CollectionChanged += (s, e) => collectionChanged += 1;

            var sut = new RoomAvailabilityProjection(vm);

            await sut.Execute(e => new EventMetadata(), 
                new RoomAdded() {Number = "101"},
                 new RoomBooked() {Number = "101"});

            sut.Model.Rooms.Should().Contain(x => x.Number == "101" && x.Reservations.Any() );
            collectionChanged.Should().Be(1);
            sut.Model.Rooms.Should().AllBeAssignableTo<INotifyPropertyChanged>();
        }
        [Fact]
        public async Task GivenWorksWithPureReadModel()
        {
            var sut = new RoomAvailabilityProjection(new RoomAvailabilityModel());

            await sut.Execute(e => new EventMetadata(),
                new RoomAdded() { Number = "101" }, new 
                    RoomBooked() { Number = "101" });

            sut.Model.Rooms.Should().Contain(x => x.Number == "101" && x.Reservations.Any());
        }
    }

    
}