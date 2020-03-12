using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.Integrations.Unity;
using EventDrivenThinking.Ui;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Xunit;

namespace EventDrivenThinking.Tests.Integration
{
    public class TypeSourceHashTests
    {

        class F
        {
            public void Foo() {  Console.WriteLine("Hello!");}
        }

        class SameAsF
        {
            public void Foo() { Console.WriteLine("Hello!"); }
        }
        class Different
        {
            public void Foo() { Console.WriteLine("Hello2!"); }
        }

        [Fact]
        public void HashReturnsSameValueForClass()
        {
            var f = typeof(F).ComputeSourceHash();
            var same = typeof(SameAsF).ComputeSourceHash();
            var different = typeof(Different).ComputeSourceHash();

            f.Should().Be(same);
            f.Should().NotBe(different);
        }
    }
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
}