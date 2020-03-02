using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonServiceLocator;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.Client;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.Integrations.EventAggregator.Client;
using EventDrivenThinking.Ui;
using EventDrivenUi.Tests.Integration;
using EventDrivenUi.Tests.Model.Hotel;
using EventDrivenUi.Tests.Model.Projections;
using EventStore.ClientAPI;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Prism.Unity;
using TechTalk.SpecFlow;
using Unity;
using Xunit;
using Xunit.Sdk;

namespace EventDrivenUi.Tests
{
    [Binding]
    public class EventDrivenSteps
    {
        private Guid _aggregateId;
        private BookRoom _bookRoomCommand;
        private ClientApp _client;
        private IAggregateEventStream<HotelAggregate> _eventStream;
        private RoomAdded _past_roomAdded;
        private RoomBooked _roomBooked;
        private ServerApp _server;
        private IUiEventBus _uiEventBus;

        private HotelViewModel _viewModel;

        private AppType type;

        [Given(@"The app is '(.*)'")]
        public void GivenTheAppIs(AppType type)
        {
            this.type = type;
            if (type == AppType.Standalone)
            {
                var container = new UnityContainer();
                _server = new ServerApp(container);
                _client = new ClientApp(container);
            }
            else if (type == AppType.ClientServer)
            {
                _server = new ServerApp();
                _client = new ClientApp();
            }
        }

        [AfterScenarioBlock()]
        public void AfterScenarioBlock()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [AfterScenario()]
        public void Cleanup()
        {
            _server?.Dispose();
            _client?.Dispose();
        }

        [Given(@"The app uses '(.*)' as eventStore")]
        public void GivenTheAppUsesAsEventStore(EventStoreMode mode)
        {
            //if (mode == EventStoreMode.InProc)
            //    _server.WriteEventPipe = w => w.ToEventAggregator();
            
            //else if (mode == EventStoreMode.EventStore)
            //    _server.WriteEventPipe = w => w.ToEventStore();
            
        }

        private bool _startHttpHost = false;

        [Given(@"The app invokes command using '(.*)'")]
        public void GivenTheAppInvokesCommandUsing(CommandInvocationTransport transport)
        {
            if (transport == CommandInvocationTransport.InProcRcp)
            {
                //_client.SendCommandPipe = s => s.ToCommandHandler();
                //_server.ReceiveCommandPipe = r => r.ToCommandHandler();
            }
            else if (transport == CommandInvocationTransport.Rest)
            {
                //_client.SendCommandPipe = s => s.ToRest();
                //_server.ReceiveCommandPipe = r => { };
                
                //_startHttpHost = true;
            }
        }

        [Given(@"The server projection use '(.*)' for subscriptions")]
        public void GivenTheServerProjectionUseForSubscriptions(ServerProjectionSubscriptionMode mode)
        {
            if (mode == ServerProjectionSubscriptionMode.EventAggregator)
            {
                if (this.type == AppType.ClientServer)
                    _server.SubscribePipe = s => s.WithEventAggregator();
            }
            else if(mode == ServerProjectionSubscriptionMode.EventStore)
                if (this.type == AppType.ClientServer)
                    _server.SubscribePipe = s =>
                    {
                        s.WithEventStore();
                        //s.ToSignalR();
                    };
        }

        [Given(@"The client projection use '(.*)' for subscriptions")]
        public void GivenTheClientProjectionUseForSubscriptions(ClientProjectionSubscriptionMode mode)
        {
            // we also need to push to SignalR 
            if (mode == ClientProjectionSubscriptionMode.SignalR)
            {
                //_client.SubscribePipe = s => s.WithSignalR("http://localhost:5000/EventHub");
            } 
            else if (mode == ClientProjectionSubscriptionMode.EventAggregator)
            {
                _client.SubscribePipe = s => s.WithEventAggregator();
            }
            else if (mode == ClientProjectionSubscriptionMode.EventStore)
            {
                _client.SubscribePipe = s => s.WithEventStore();
            }
        }


        [Given(@"I've defined past from the events")]
        public async Task GivenIVeDefinedPastFromTheEvents()
        {
            _past_roomAdded = new RoomAdded {Number = "101"};
            _aggregateId = Guid.NewGuid();
            await _eventStream.Append(_aggregateId, ExpectedVersion.Any, Guid.NewGuid(), _past_roomAdded);

            var events = _eventStream.Get(_aggregateId);
            var aggregate = new HotelAggregate();
            aggregate.Id = _aggregateId;
            await aggregate.RehydrateAsync(events);

            aggregate.Value.Id.Should().Be(_aggregateId);
            aggregate.Value.AvailableRooms.Should().NotBeEmpty();
        }

        [Given(@"I've build the pipelines")]
        public void GivenIVeBuildThePipelines()
        {
            if (_startHttpHost && type == AppType.ClientServer)
            {
                _server.ConfigureHost();
                _server.StartHost();

                var sp = _server.ServiceProvider;
                sp = sp.GetService<IServiceProvider>();
                sp.AssertTryResolveService<IAggregateEventStream<HotelAggregate>>();
                sp.AssertTryResolveClass<RoomAvailabilityProjection>();
            }
            
            if(!_startHttpHost && type == AppType.Standalone)
            {
                _server.InitializeContainer();
                _client.InitializeContainer();

                _server.ConfigurePlumbing();
                _server.ConnectPipes();

                _client.ConfigurePlumbing();
                _client.ConnectPipes();
            }

            _eventStream = _server.Resolve<IAggregateEventStream<HotelAggregate>>();
            _uiEventBus = _client.Resolve<IUiEventBus>();
            
        }

        [Given(@"I setup view-model")]
        public void GivenISetupView_Model()
        {
            _viewModel = new HotelViewModel(() => new ViewModelRoomAvailabilityModel(), _aggregateId);
        }


        [When(@"I invoke commands")]
        public void WhenIInvokeCommands()
        {
            _viewModel.Commands.BookRoom.Execute();
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [Then(@"The command-ui-event is published")]
        public void ThenTheCommand_Ui_EventIsPublished()
        {
            _uiEventBus.PublishedEvents.OfType<CommandEnvelope<Guid, BookRoom>>()
                .Should().HaveCount(1);
        }


        [Then(@"The event is published")]
        public void ThenTheEventIsPublished()
        {
            _viewModel.Rooms.All(x => x.Reservations.Any())
                .Should().Be(true);
        }

        [Then(@"The aggregate gets the command")]
        public async Task ThenTheAggregateGetsTheCommand()
        {
            var events = _eventStream.Get(_aggregateId);
            var aggregate = new HotelAggregate();
            aggregate.Id = _aggregateId;
            await aggregate.RehydrateAsync(events);

            aggregate.Value.Id.Should().Be(_aggregateId);
            aggregate.Value.AvailableRooms.Should().BeEmpty();
        }

        [Then(@"The projection gets the event")]
        public void ThenTheProjectionGetsTheEvent()
        {
            // Hard to check
        }

        [Then(@"The view-model gets changes")]
        public void ThenTheView_ModelGetsChanges()
        {
            // waiting for subscription to process
            Thread.Sleep(2000);
            var model = _client.Resolve<IRoomAvailabilityModel>();

            model.Rooms.Should().HaveCount(1);
            model.Rooms.First().Reservations.Should().HaveCount(1);
        }
    }
}