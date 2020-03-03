using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.Example.Model.Hotel;
using EventDrivenThinking.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace EventDrivenThinking.Tests
{
    public class FastCreateTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FastCreateTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        interface IInvoke
        {
            void Foo(int a);
        }

        class Invoke<T> : IInvoke
        {
            public int a;
            public void Foo(int a)
            {
                this.a = a;
            }
        }
        [Fact]
        public void TestFactCreate()
        {
            var instance = Ctor<IInvoke>.Create(typeof(Invoke<int>));

            instance.Foo(10);

            instance.Should().BeOfType<Invoke<int>>();
        }

        

        

    }
    public class HotelAggregateTests
    {
        [Fact]
        public void ExecuteWorks()
        {
            HotelAggregate hotel = new HotelAggregate();
            hotel.Rehydrate(new RoomAdded(){Number = "101"});
            var e = hotel.Execute(new BookRoom());
            e[0].Should().BeOfType<RoomBooked>();
        }

        [Fact]
        public void ExecuteWorksWithSimpleEvents()
        {
            HotelAggregate hotel = new HotelAggregate();
            hotel.Rehydrate(new RoomAdded() { Number = "101" });
            var e = hotel.Execute(new CloseRoom());
            e[0].Should().BeOfType<RoomClosed>();
        }

        [Fact]
        public void ReplayWorks()
        {
            HotelAggregate hotel = new HotelAggregate();
            hotel.Rehydrate(new RoomAdded() {Number="101"}, new RoomBooked(){Number = "101"});
            hotel.Version.Should().Be(1);
        }
    }
}
