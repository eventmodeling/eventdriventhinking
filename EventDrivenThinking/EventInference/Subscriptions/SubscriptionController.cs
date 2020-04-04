using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface IProjectionSubscriptionController
    {
        Task SubscribeHandlers(IProjectionSchema schema, IEventHandlerFactory factory, params object[] args);
    }

    public interface IProjectionStreamSubscriptionController
    {
        Task SubscribeHandlers(IProjectionSchema schema, IEventHandlerFactory factory, params object[] args);
    }

    public class ProjectionStreamSubscriptionController : SubscriptionController<IProjectionEventStream, IProjectionSchema>, IProjectionStreamSubscriptionController
    {
        public ProjectionStreamSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
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

        
        public virtual async Task SubscribeHandlers(TSchema schema, IEventHandlerFactory factory, params object[] args)
        {
            var providers = OnConstructSubscriptionProviders(factory.SupportedEventTypes, args);

            foreach(var i in providers)
            {
                await i.Subscribe(schema, factory, args);
            }
        }

        /// <summary>
        /// Memory allocation in runtime - subject for refactor. O(N^2)
        /// </summary>
        /// <param name="types"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual ISubscriptionProvider<TOwner, TSchema>[] OnConstructSubscriptionProviders(TypeCollection types, object[] args)
        {
            List<ISubscriptionProvider<TOwner, TSchema>> providers = new List<ISubscriptionProvider<TOwner, TSchema>>(types.Count);
            foreach (var et in types)
            {
                var requestedInterface = typeof(IEventSubscriptionProvider<,,>).MakeGenericType(typeof(TOwner), typeof(TSchema), et);

                var subscriptionProvider = (ISubscriptionProvider<TOwner, TSchema>) _serviceProvider.GetRequiredService(requestedInterface);

                providers.Add(subscriptionProvider);
            }

            if (providers.Count <= 1) 
                return providers.ToArray();

            for (int i = 0; i < providers.Count; i++)
            {
                var tmp = providers[i];

                for (int j = 1; j < providers.Count; j++)
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