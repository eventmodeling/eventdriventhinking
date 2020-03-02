using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using EventDrivenThinking.Reflection;
using FluentAssertions;
using Xunit;
[assembly: InternalsVisibleTo("DynamicMarkupAssembly")]

namespace EventDrivenUi.Tests.Convention2Interface
{
    interface IFoo2<out D, in T>
    {
        D Hello(T arg);
    }
    interface IFoo<in T, out D>
    {
        D Hello(T arg);
    }

    internal class Foo
    {
        internal virtual string Hello(string arg)
        {
            return arg;
        }
    }

    

    public class MarkupClassFactoryTests
    {
        [Fact]
        public void CheckInheritance()
        {
            MarkupOpenGenericFactory openGenericFactory = new MarkupOpenGenericFactory(typeof(Foo), typeof(IFoo<,>));

            var sut = openGenericFactory.Create<IFoo<string,string>>();

            sut.Hello("hello").Should().Be("hello");

           
        }

        [Fact]
        public void CheckInverseInheritance()
        {
            MarkupOpenGenericFactory openGenericFactory2 = new MarkupOpenGenericFactory(typeof(Foo), typeof(IFoo2<,>));

            var sut2 = openGenericFactory2.Create<IFoo2<string, string>>();

            var actual = sut2.Hello("hello");
            sut2.Hello("hello").Should().Be("hello");
        }
    }
}
