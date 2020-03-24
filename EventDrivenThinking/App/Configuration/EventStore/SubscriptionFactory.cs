using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using EventTypeFilter = EventStore.Client.EventTypeFilter;

namespace EventDrivenThinking.App.Configuration.EventStore
{
    class SubscriptionFactory
    {
        private readonly IEventStoreFacade _connection;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task>> _methods;
        private readonly MethodInfo _onSpecMeth;
        
        public SubscriptionFactory(IEventStoreFacade connection, IServiceProvider serviceProvider)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
            _methods = new Dictionary<string, Func<IStreamSubscription, ResolvedEvent, CancellationToken,Task>>();
            _onSpecMeth = this.GetType().GetMethod(nameof(OnSpecEventAppearead),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        }


        public async Task SubscribeToStreams(params SubscriptionInfo[] eventTypes)
        {
            var prefixes = eventTypes.Select(x => x.EventType.Name).ToArray();
            var filter = EventTypeFilter.Prefix(prefixes);
            FilterOptions filterOptions = new FilterOptions(filter);

            var handlers = eventTypes.ToDictionary(x => x.EventType.Name, x => x.HandlerType);
            var dict = eventTypes.Select(x => x.EventType).ToDictionary(x => x.Name);
            await _connection.SubscribeToAllAsync(Position.Start, async (s,r,c) =>
            {
                await OnEventAppearead(s, r, handlers, dict, c);
            }, true, OnDropped, filterOptions);
        }

        private DateTimeOffset _continuum;
        private void OnDropped(IStreamSubscription arg1, SubscriptionDroppedReason arg2, Exception arg3)
        {

        }
        
        
        private async Task OnEventAppearead(IStreamSubscription arg1, 
            ResolvedEvent arg2, 
            IDictionary<string, Type> handlers,
            IDictionary<string, Type> eventToTypeDict,
            CancellationToken arg3)
        {
            var eventType = arg2.Event.EventType;
            if (!_methods.TryGetValue(eventType, out var func))
            {
                var p1= Expression.Parameter(typeof(IStreamSubscription), "arg1");
                var p2= Expression.Parameter(typeof(ResolvedEvent), "arg2");
                var p3 = Expression.Parameter(typeof(CancellationToken), "arg3");

                var t = Expression.Constant(this);
                var et = eventToTypeDict[eventType];
                var h = handlers[eventType];

                var c = Expression.Call(t, _onSpecMeth.MakeGenericMethod(et, h), p1, p2, p3);
                func = Expression.Lambda<Func<IStreamSubscription, ResolvedEvent, CancellationToken, Task>>(c, new []{p1,p2,p3})
                    .Compile();
                _methods.Add(eventType, func);
            }

            await func(arg1, arg2, arg3);
        }
        private async Task OnSpecEventAppearead<TEvent,TEventHandler>(IStreamSubscription arg1, ResolvedEvent arg2, CancellationToken arg3)
        where TEvent:IEvent
        where TEventHandler : IEventHandler<TEvent>
        {
            var eventData = Encoding.UTF8.GetString(arg2.Event.Data);
            var metaData = Encoding.UTF8.GetString(arg2.Event.Metadata);

            var ev = JsonConvert.DeserializeObject<TEvent>(eventData);
            var m = JsonConvert.DeserializeObject<EventMetadata>(metaData);

            if (_continuum < m.TimeStamp)
                _continuum = m.TimeStamp;
            else throw new InvalidOperationException();

            m.Version = arg2.Event.EventNumber;
            Debug.WriteLine($"==========> EventAppeared {m.TimeStamp}: {m.Version} {ev.GetType().Name}");
            //_lastCheckpoint = arg2.OriginalEventNumber;
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var handler = ActivatorUtilities.CreateInstance<TEventHandler>(scope.ServiceProvider);
                await handler.Execute(m, ev);
            }
        }
    }
}
