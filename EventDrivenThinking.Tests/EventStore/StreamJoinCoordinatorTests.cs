using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.Fresh.EventStore;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Example.Model.Hotel;
using EventStore.ClientAPI;
using FluentAssertions;
using NSubstitute;
using Serilog.Core;
using Xunit;

namespace EventDrivenThinking.Tests.EventStore
{
    public class StreamJoinCoordinatorTests
    {
        class DispatcherMock : IEventHandlerDispatcher
        {
            public SynchronizedCollection<(EventMetadata, IEvent)> Dispatched = new SynchronizedCollection<(EventMetadata, IEvent)>();
            public Task Dispatch<TEvent>(EventMetadata m, TEvent ev) where TEvent : IEvent
            {
                Dispatched.Add((m,ev));
                Debug.WriteLine($"Dispatched {m.TimeStamp.Ticks - n.Ticks}");
                return Task.CompletedTask;
            }

            private DateTimeOffset n;

            public DispatcherMock(DateTimeOffset n)
            {
                this.n = n;
            }
        }
        private DispatcherMock dispatcher;
        private StreamJoinCoordinator sut;

        private List<EventMetadata> dispatchedEvents;
        DateTimeOffset n = DateTimeOffset.Now;
        private IServiceProvider serviceProvider;

        public StreamJoinCoordinatorTests()
        {
            this.dispatcher = new DispatcherMock(n);
            this.serviceProvider = Substitute.For<IServiceProvider>();
            this.sut = new StreamJoinCoordinator(Substitute.For<IEventStoreConnection>(),
                Logger.None, serviceProvider);
            dispatchedEvents = new List<EventMetadata>();
        }
        [Fact]
        public async Task OrderingWorks()
        {

            // creating 3 receivers: 0,1,2
            //sut.SubscribeToStreams(new Type[] { typeof(RoomClosed), typeof(RoomBooked),typeof(RoomAdded) });

            

            var s0 = Task.Run(async () => await PushEvent(1,new RoomBooked(), n));

            s0.IsCompleted.Should().BeFalse();

            var s1 = Task.Run(async () => await PushEvent(2,new RoomAdded(), n.AddTicks(-1)));
            s1.IsCompleted.Should().BeFalse();

            var s2 = Task.Run(async () => await PushEvent(0,new RoomClosed(), n.AddTicks(1)));
            await s1;
            s0.IsCompleted.Should().BeFalse();
            s2.IsCompleted.Should().BeFalse();

            // subscription '2' is going live
            sut.ReceiverIsLive(2);
            await s0;
            s2.IsCompleted.Should().BeFalse();

            // subscription '1' is going live
            sut.ReceiverIsLive(1);

            await Task.Delay(1000);
            Debug.WriteLine("==========");
            await Task.Delay(1000);

            // if live subscription is by any chance slower than catchup than it should be as soon as possible.
            var s5 = Task.Run(async () => await PushEvent(0, new RoomClosed(), n.AddTicks(4)));
            await Task.Delay(200);
            var s4 = Task.Run(async () => await PushEvent(2, new RoomAdded(), n.AddTicks(3)));
            await s4;
            await s5;

            sut.ReceiverIsLive(0);
            await Task.Delay(100);
            sut.Count.Should().Be(0);
            sut.IsLive.Should().BeTrue();
            int dispached = dispatcher.Dispatched.Count;

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(async () => await PushEvent(0, new RoomClosed(), n.AddTicks(5+3 * i))));
                tasks.Add(Task.Run(async () => await PushEvent(2, new RoomAdded(), n.AddTicks(6+ 3 * i))));
                tasks.Add(Task.Run(async () => await PushEvent(1, new RoomBooked(), n.AddTicks(7+ 3 * i))));
            }

            await Task.WhenAll(tasks.ToArray());
            
            dispatcher.Dispatched.Count.Should().Be(dispached + 300);

            dispatcher.Dispatched[0].Item1.TimeStamp.Should().Be(n.AddTicks(-1));
            dispatcher.Dispatched[1].Item1.TimeStamp.Should().Be(n);
            dispatcher.Dispatched[2].Item1.TimeStamp.Should().Be(n.AddTicks(1));
            dispatcher.Dispatched[4].Item1.TimeStamp.Should().Be(n.AddTicks(3));
            dispatcher.Dispatched[3].Item1.TimeStamp.Should().Be(n.AddTicks(4));
        }

        private async Task PushEvent<TEvent>(int nr,TEvent ev, DateTimeOffset at) where TEvent : IEvent
        {
            await sut.Push(nr, new EventMetadata(Guid.NewGuid(), typeof(HotelAggregate), Guid.NewGuid())
            {
                TimeStamp = at
            }, ev);
        }
    }
}
