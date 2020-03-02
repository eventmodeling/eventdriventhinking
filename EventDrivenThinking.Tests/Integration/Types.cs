using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenThinking.Ui;
using EventDrivenUi.Tests.Model.Hotel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Unity;
using Unity.Lifetime;
using Xunit;

namespace EventDrivenUi.Tests.Integration
{
    public interface IFoo
    {

    }
    public class Foo : IFoo { }
    public class Foo2 : IFoo { }
    public class UnityServiceScopeFactoryTests
    {
        [Fact]
        public void UnityIsRegistered()
        {
            UnityContainer c = new UnityContainer();
            c.RegisterInstance<IServiceScopeFactory>(new UnityServiceScopeFactory(c));
            var sp = new UnityServiceProvider(c);
            c.RegisterInstance<IServiceProvider>(sp);
            c.RegisterSingleton<IFoo, Foo>();

            c.IsRegistered(typeof(IFoo)).Should().BeTrue();
        }

        [Fact]
        public void UnityNotEnumerable()
        {
            UnityContainer c = new UnityContainer();
            c.RegisterInstance<IServiceScopeFactory>(new UnityServiceScopeFactory(c));
            var sp = new UnityServiceProvider(c);
            c.RegisterInstance<IServiceProvider>(sp);
            c.RegisterSingleton<IFoo, Foo>();
            
            var service = sp.GetRequiredService<IFoo>();
            service.Should().NotBeNull();
        }

        [Fact]
        public void UnityEnumerable()
        {
            UnityContainer c = new UnityContainer();
            c.RegisterInstance<IServiceScopeFactory>(new UnityServiceScopeFactory(c));
            var sp = new UnityServiceProvider(c);
            c.RegisterInstance<IServiceProvider>(sp);
            c.RegisterSingleton<IFoo, Foo>("a");
            c.RegisterSingleton<IFoo, Foo2>("b");


            var services = sp.GetRequiredService<IEnumerable<IFoo>>().ToArray();
            services.Length.Should().Be(2);
        }

        [Fact]
        public void Singleton()
        {
            UnityContainer c = new UnityContainer();
            c.RegisterInstance<IServiceScopeFactory>(new UnityServiceScopeFactory(c));
            var sp = new UnityServiceProvider(c);
            c.RegisterInstance<IServiceProvider>(sp);
            c.RegisterSingleton<IFoo, Foo>();

            IFoo expected = c.Resolve<IFoo>();
            IFoo actual = null;
            using (var scope = sp.CreateScope())
            {
                actual = scope.ServiceProvider.GetService<IFoo>();
            }

            actual.Should().Be(expected);
        }

        [Fact]
        public void ModelFactoryTest()
        {
            UnityContainer c = new UnityContainer();
            c.RegisterInstance<IServiceScopeFactory>(new UnityServiceScopeFactory(c));
            var sp = new UnityServiceProvider(c);
            c.RegisterInstance<IServiceProvider>(sp);

            
            c.RegisterFactory<Foo>(uc => uc.Resolve<IModelFactory>().Create<Foo>(), FactoryLifetime.Singleton);

            IModelFactory m = new UiModelFactory(sp);
            c.RegisterInstance<IModelFactory>(m);
            var expected = c.Resolve<Foo>();
            var actual = c.Resolve<Foo>();

            actual.Should().Be(expected);
        }

    }

    public class SerializationTests
    {
       
        [Fact]
        public void EventMetadataCanBeSerialized()
        {
            EventMetadata m = new EventMetadata(Guid.NewGuid(), typeof(HotelAggregate), Guid.NewGuid());
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
