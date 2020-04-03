using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public class SubscriptionController<TOwner> // TOwner can be specyfic projection or processor.
    {
        private readonly IServiceProvider _serviceProvider;

        // we cache only when we don't have any args.
        
        public SubscriptionController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        
        }

        
        public async Task SubscribeHandlers(ISchema schema, IEventHandlerFactory factory, object[] args = null)
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
        private ISubscriptionProvider<TOwner>[] OnConstructSubscriptionProviders(TypeCollection types, object[] args)
        {
            List<ISubscriptionProvider<TOwner>> providers = new List<ISubscriptionProvider<TOwner>>(types.Count);
            foreach (var et in types)
            {
                var requestedInterface = typeof(IEventSubscriptionProvider<,>).MakeGenericType(typeof(TOwner), et);

                var subscriptionProvider = (ISubscriptionProvider<TOwner>) ActivatorUtilities.CreateInstance(_serviceProvider, requestedInterface);

                providers.Add(subscriptionProvider);
            }


            for (int i = 0; i < providers.Count; i++)
            {
                var tmp = providers[i];

                for (int j = 1; i < providers.Count; j++)
                {
                    var other = providers[j];
                    if (tmp.CanMerge(other))
                    {
                        // other is not usefull
                        providers[i] = tmp.Merge(other);
                        providers.RemoveAt(j--);
                    }
                }
            }

            return providers.ToArray();
        }
    }
}