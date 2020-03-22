using System;
using System.Windows;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace EventDrivenThinking.Tests.Integration
{
    public interface IFoo
    {

    }
    public class Foo : IFoo { }
    public class Foo2 : IFoo { }

    public class SerializationTests
    {
       
        [Fact]
        public void EventMetadataCanBeSerialized()
        {
            EventMetadata m = new EventMetadata(Guid.NewGuid(), typeof(HotelAggregate), Guid.NewGuid(),0);
            var str = JsonConvert.SerializeObject(m);
            var actual = JsonConvert.DeserializeObject<EventMetadata>(str);
            actual.Should().BeEquivalentTo(m);
        }

        

        [Fact]
        public void NestedPoints()
        {
            Y test = new Y() { X = new Point(1,1)};

            string str = JsonConvert.SerializeObject(test);

            Y actual = JsonConvert.DeserializeObject<Y>(str);

            actual.Should().BeEquivalentTo(test);
        }
    }

    public class Y
    {
        public Point X { get; set; }
    }
    public enum AppType
    {
        Standalone,
        ClientServer
    }
    public enum EventStoreMode
    {
        EventStore,
        InProc
    }

    public enum CommandInvocationTransport
    {
        InProcRcp,
        Rest
    }

    public enum ServerProjectionSubscriptionMode
    {
        EventAggregator,
        EventStore
    }

    public enum ClientProjectionSubscriptionMode
    {
        EventAggregator,
        EventStore,
        SignalR
    }
}
