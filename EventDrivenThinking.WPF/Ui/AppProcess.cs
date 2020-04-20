using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommonServiceLocator;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Ui.Obsolete;
using Prism.Events;

namespace EventDrivenThinking.Ui
{
    
    
    /// <summary>
    /// Only on client site
    /// </summary>
    public abstract class AppProcess : IAppProcess
    {
        protected readonly IEventAggregator _eventAggregator;
        protected readonly IUiEventBus _bus;
        protected AppProcess()
        {
            _eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            _bus = ServiceLocator.Current.GetInstance<IUiEventBus>();
            WireEvents();
        }
        const BindingFlags DEFAULT_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        private void WireEvents()
        {
            
            var evMth = typeof(AppProcess)
                .GetMethod(nameof(WireWithEvent), DEFAULT_FLAGS);

            var cmdMth = typeof(AppProcess)
                .GetMethod(nameof(WireWithCommand), DEFAULT_FLAGS);

            var methods = GetType().GetMethods(DEFAULT_FLAGS)
                .Where(x => x.Name == "When")
                .ToArray();

            WireWhensWithEvents(evMth, methods);
            WireWhensWithCommands(cmdMth, methods);
        }

        private void WireWhensWithCommands(MethodInfo cmdWireMth, IEnumerable<MethodInfo> mCollection)
        {
            var methods = mCollection
                .Where(x => x.GetParameters().Length == 1)
                .ToArray();

            foreach (var m in methods)
                cmdWireMth.MakeGenericMethod(m.GetParameters()[0].ParameterType)
                    .Invoke(this, new object[] { m });
        }
        private void WireWhensWithEvents(MethodInfo evWireWhen, IEnumerable<MethodInfo> mCollection)
        {
            var methods = mCollection
                .Where(x => x.GetParameters().Length == 2)
                .ToArray();

            foreach (var m in methods)
                evWireWhen.MakeGenericMethod(m.GetParameters()[1].ParameterType)
                    .Invoke(this, new object[] {m});

        }
        private void WireWithEvent<TEvent>(MethodInfo minfo) where TEvent : IEvent
        {
            var func = (Func<EventMetadata, TEvent, IEnumerable<IEvent>>)minfo.CreateDelegate(
                typeof(Func<EventMetadata, TEvent, IEnumerable<IEvent>>), this);
            var option = ThreadOption.BackgroundThread;

            var att = minfo.GetCustomAttribute<SubscriptionOptionsAttribute>();
            if (att != null) option = att.Option;

            _eventAggregator.GetEvent<PubSubEvent<EventEnvelope<TEvent>>>().Subscribe(cmd =>
            {
                var events = func(cmd.Metadata, cmd.Event);
                foreach (var e in events)
                    PublishEvent(e);
            }, option, true);
        }
        private void WireWithCommand<TCommand>(MethodInfo minfo)
        {
            var func = (Func<TCommand, IEnumerable<IEvent>>) minfo.CreateDelegate(
                typeof(Func<TCommand, IEnumerable<IEvent>>), this);
            var option = ThreadOption.BackgroundThread;

            var att = minfo.GetCustomAttribute<SubscriptionOptionsAttribute>();
            if (att != null) option = att.Option;

            _eventAggregator.GetEvent<PubSubEvent<TCommand>>().Subscribe(cmd =>
            {
                var events = func(cmd);
                foreach (var e in events)
                    PublishEvent(e);
            }, option, true);
        }

        private void PublishEvent(IEvent @event)
        {
            // SLOW
            var type = @event.GetType();
            var minfo = typeof(AppProcess).GetMethod("PublishEventInternal",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(type);

            minfo.Invoke(this, new object[] {@event});
        }

        private void PublishEventInternal<TEvent>(TEvent ev)
        {
            _bus.GetEvent<TEvent>().Publish(ev);
        }
    }
}