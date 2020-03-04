using System.ComponentModel;
using AutoFixture;
using AutoMapper;
using EventDrivenThinking.Example.Ui;
using EventDrivenThinking.Ui;
using FluentAssertions;
using Xunit;

namespace EventDrivenThinking.Tests
{
    public class NotifyPropertyChangeProxyFactoryTests
    {
        Fixture _fixture = new Fixture();

        [Fact]
        public void GetShouldBePropagated()
        {
            var model = _fixture.Create<Model>();
            model.NestedItems.Add(new ClassX());

            var actual = Create(model);

            actual.Should().BeEquivalentTo(model);
        }

        [Fact]
        public void ProxyShouldNotRiseEventsWhenValuesAreSame()
        {
            var model = _fixture.Create<Model>();

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
            var model = _fixture.Create<Model>();

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
            var model = _fixture.Create<Model>();

            var actual = Create(model);

            actual.Should().BeAssignableTo<INotifyPropertyChanged>();
        }

        private Model Create(Model root)
        {
            
            var derived = ViewModelFactory<Model>.Create();

            INotifyPropertyChanged nc = (INotifyPropertyChanged) derived;
            
            var config = new MapperConfiguration(cfg => cfg.CreateMap(root.GetType(), derived.GetType()));
            var mapper = config.CreateMapper();
            // or
            
            var vm = (Model)mapper.Map(root, root.GetType(), derived.GetType());

            foreach (var i in root.NestedItems) vm.NestedItems.Add(i);

            return vm;
        }
    }
}
