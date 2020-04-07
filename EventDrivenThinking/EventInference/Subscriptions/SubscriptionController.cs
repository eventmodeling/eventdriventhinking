using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.SessionManagement;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface ISubscription
    {
        Task Catchup();
        ISubscription Merge(ISubscription single);
    }

    class MultiSubscription : ISubscription
    {
        private readonly List<ISubscription> _subscriptions;

        public MultiSubscription(params ISubscription[] subscriptions)
        {
            _subscriptions = new List<ISubscription>(subscriptions);
        }
        public ISubscription Merge(ISubscription single)
        {
            _subscriptions.Add(single);
            return this;
        }

        public Task Catchup()
        {
            var s = _subscriptions.Select(x=>x.Catchup()).ToArray();
            return Task.WhenAll(s);
        }
    }
    class Subscription : ISubscription
    {
        private readonly AsyncManualResetEvent _catchup;

        public Subscription(bool isLive = false)
        {
            _catchup = new AsyncManualResetEvent(isLive);
        }

        public void MakeLive()
        {
            _catchup.Set();
        }
        public Task Catchup()
        {
            //_catchup.Wait();
            return _catchup.WaitAsync();
            //return Task.CompletedTask;
        }

        public ISubscription Merge(ISubscription single)
        {
            if (single is MultiSubscription)
                return single.Merge(this);
            else return new MultiSubscription(this, single);
        }
    }
    public interface IProjectionSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProjectionSchema schema, 
            IEventHandlerFactory factory,
            params object[] args);
    }

    public interface IProjectionStreamSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProjectionSchema schema, IEventHandlerFactory factory, params object[] args);
    }

    public class ProjectionStreamSubscriptionController : SubscriptionController<IProjectionEventStream, IProjectionSchema>, IProjectionStreamSubscriptionController
    {
        public ProjectionStreamSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public interface IProcessorSubscriptionController
    {
        Task<ISubscription> SubscribeHandlers(IProcessorSchema schema, IEventHandlerFactory factory, params object[] args);
    }

    public class ProcessorSubscriptionController : SubscriptionController<IProcessor, IProcessorSchema>, IProcessorSubscriptionController
    {
        public ProcessorSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
    public class ProjectionSubscriptionController : SubscriptionController<IProjection, IProjectionSchema>, 
        IProjectionSubscriptionController
    {
        public ProjectionSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
    /// <summary>
    /// Optimizes subscription creation. Uses factory to understand what subscription providers to use.
    /// And then subscribes to all of them. Matching is done based on event-from-factory and T-Schema & T-Owner.
    /// </summary>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TSchema"></typeparam>
    public abstract class SubscriptionController<TOwner, TSchema>
    where TSchema : ISchema
    {
        private readonly IServiceProvider _serviceProvider;

        // we cache only when we don't have any args.
        
        public SubscriptionController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        
        }

        
        public virtual async Task<ISubscription> SubscribeHandlers(TSchema schema, IEventHandlerFactory factory, params object[] args)
        {
            var providers = OnConstructSubscriptionProviders(factory.SupportedEventTypes, schema, args);
            MultiSubscription multi = new MultiSubscription();
            foreach(var i in providers)
            {
                multi.Merge(await i.Subscribe(factory, args));
            }

            return multi;
        }

        /// <summary>
        /// Memory allocation in runtime - subject for refactor. O(N^2)
        /// </summary>
        /// <param name="types"></param>
        /// <param name="schema"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual ISubscriptionProvider<TOwner, TSchema>[] OnConstructSubscriptionProviders(
            TypeCollection types, TSchema schema, object[] args)
        {
            List<ISubscriptionProvider<TOwner, TSchema>> providers = new List<ISubscriptionProvider<TOwner, TSchema>>(types.Count);
            foreach (var et in types)
            {
                var requestedInterface = typeof(IEventSubscriptionProvider<,,>).MakeGenericType(typeof(TOwner), typeof(TSchema), et);

                var subscriptionProvider = (ISubscriptionProvider<TOwner, TSchema>) _serviceProvider.GetRequiredService(requestedInterface);
                subscriptionProvider.Init(schema);

                providers.Add(subscriptionProvider);
            }

            if (providers.Count <= 1) 
                return providers.ToArray();

            for (int i = 0; i < providers.Count; i++)
            {
                var tmp = providers[i];

                for (int j = i+1; j < providers.Count; j++)
                {
                    var other = providers[j];
                    if (tmp.CanMerge(other))
                    {
                        // other is not usefull
                        providers[i] = tmp = tmp.Merge(other);
                        providers.RemoveAt(j--);
                    }
                }
            }

            return providers.ToArray();
        }
    }
}