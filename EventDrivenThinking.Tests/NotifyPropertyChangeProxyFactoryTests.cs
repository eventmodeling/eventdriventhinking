using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui;
using EventDrivenUi.Example;
using FluentAssertions;
using Serilog;
using Unity.Injection;
using Xunit;

namespace EventDrivenUi.Tests
{
    public class NotifyPropertyChangeProxyFactoryTests
    {
        Fixture _fixture = new Fixture();

        [Fact]
        public void GetShouldBePropagated()
        {
            var model = _fixture.Create<Example.Model>();
            model.NestedItems.Add(new ClassX());

            var actual = Create(model);

            actual.Should().BeEquivalentTo(model);
        }

        [Fact]
        public void ProxyShouldNotRiseEventsWhenValuesAreSame()
        {
            var model = _fixture.Create<Example.Model>();

            var actual = Create(model);
            actual.StringArg = "NewString";

            

            var npch = (INotifyPropertyChanged)actual;
            int invoked = 0;
            string propName = "";
            npch.PropertyChanged += (s, e) =>
            {
                invoked++;
                propName = e.PropertyName;
            };
            actual.StringArg = "NewString";

            invoked.Should().Be(0);
        }

        [Fact]
        public void ProxyShouldRiseEvents()
        {
            var model = _fixture.Create<Example.Model>();

            var actual = Create(model);

            var npch = (INotifyPropertyChanged) actual;
            int invoked = 0;
            string propName = "";
            npch.PropertyChanged += (s,e) =>
            {
                invoked++;
                propName = e.PropertyName;
            };

            actual.StringArg = "NewString";

            propName.Should().Be("StringArg");
            invoked.Should().Be(1);
        }

        [Fact]
        public void ProxyShouldImplementNotifyPropertyChanged()
        {
            var model = _fixture.Create<Example.Model>();

            var actual = Create(model);

            actual.Should().BeAssignableTo<INotifyPropertyChanged>();
        }

        private Example.Model Create(Example.Model root)
        {
            
            var derived = ViewModelFactory<Example.Model>.Create();

            INotifyPropertyChanged nc = (INotifyPropertyChanged) derived;
            
            var config = new MapperConfiguration(cfg => cfg.CreateMap(root.GetType(), derived.GetType()));
            var mapper = config.CreateMapper();
            // or
            
            var vm = (Example.Model)mapper.Map(root, root.GetType(), derived.GetType());

            foreach (var i in root.NestedItems) vm.NestedItems.Add(i);

            return vm;
        }
    }
}
