using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using EventStore.Client;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace EventDrivenThinking.Tests.Integration
{
    public class EventStoreFacadeTests
    {
        IEnumerable<EventData> TestEvents(params object[] data)
        {
            foreach (var i in data)
            {
                yield return TestEvent(Guid.NewGuid(), i.GetType().Name, i);
            }
        }

        EventData TestEvent(Guid id, string type, object data)
        {
            return new EventData(
                Uuid.FromGuid(id), type, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))
            );
        }

        [Fact]
        public async Task StreamRevisionWorksSameForTcpAndHttp()
        {
            EventStoreFacade f = ConnectEs();

            Guid g = Guid.NewGuid();
            string sm = $"test-{g}";
            List<ResolvedEvent> events_1 = new List<ResolvedEvent>(); 
            List<ResolvedEvent> events_2 = new List<ResolvedEvent>();
            StreamRevision sr = new StreamRevision(1);

            await f.AppendToStreamAsync(sm, AnyStreamRevision.Any, TestEvents(new RoomAdded(), new RoomAdded()));

            await foreach(var i in f.ReadStreamAsync(Direction.Forwards, sm, StreamRevision.Start, 2))
                events_1.Add(i);

            f.IsTcp = !f.IsTcp;

            await foreach (var i in f.ReadStreamAsync(Direction.Forwards, sm, StreamRevision.Start, 2))
                events_2.Add(i);

            for (int i = 0; i < 2; i++)
            {
                var e1 = events_1[i];
                var e2 = events_2[i];

                e1.OriginalEventNumber.Should().Be(e2.OriginalEventNumber);
            }
        }

        [Fact]
        public async Task SubscribeFromTime()
        {
            EventStoreFacade f = ConnectEs();

            Guid g = Guid.NewGuid();
            string sm = $"test-{g}";
            List<ResolvedEvent> events = new List<ResolvedEvent>();
            StreamRevision sr = new StreamRevision(1);

            await f.AppendToStreamAsync(sm, AnyStreamRevision.Any, TestEvents(new RoomAdded(), new RoomAdded()));

            await f.SubscribeToStreamAsync(sm, sr, async (s, r, c) => { events.Add(r); }, null, true);

            await Task.Delay(1000);

            events.Should().HaveCount(1);
        }
        [Fact]
        public async Task SubscribeFromStart()
        {
            EventStoreFacade f = ConnectEs();

            Guid g = Guid.NewGuid();
            string sm = $"test-{g}";
            List<ResolvedEvent> events = new List<ResolvedEvent>();
            StreamRevision sr = new StreamRevision(0);

            await f.SubscribeToStreamAsync(sm, sr, async (s, r, c) => { events.Add(r); }, null, true);

            await f.AppendToStreamAsync(sm, AnyStreamRevision.Any, TestEvents(new RoomAdded()));

            await Task.Delay(1000);

            events.Should().HaveCount(1);
        }

        private EventStoreFacade ConnectEs()
        {
            EventStoreFacade es = new EventStoreFacade("https://localhost:2113",
                "tcp://localhost:1113", "admin", "changeit");
            return es;
        }

    }
}