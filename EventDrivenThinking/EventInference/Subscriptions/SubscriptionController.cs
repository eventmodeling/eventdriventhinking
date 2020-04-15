using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.SessionManagement;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Subscriptions
{
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

        
        public virtual async Task<ISubscription> SubscribeHandlers(TSchema schema, 
            IEventHandlerFactory factory, 
            Predicate<ISubscriptionProvider<TOwner, TSchema>> filter = null,
            params object[] args)
        {
            if (filter == null) 
                filter = (x) => true;

            // We optimize list of providers
            // We usually aggregate providers from same infrastructure into one. 
            var providers = OnConstructSubscriptionProviders(factory.SupportedEventTypes, schema, args);
            
            
            MultiSubscription multi = new MultiSubscription();
            foreach(var i in providers.Where(i => filter(i)))
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