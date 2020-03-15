using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration
{
    public static class EventDrivenThinkingConfigurationExtension
    {
        
        internal static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            foreach (var i in items) set.Add(i);
        }
        internal static void Merge<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            foreach (var i in items) if (!set.Contains(i)) set.Add(i);
        }

        public static Configuration AddEventDrivenThinking(this IServiceCollection collection, ILogger logger, Action<Configuration> config)
        {
            Bootstrapper b = new Bootstrapper(logger, collection);
            
            b.Register(config);

            return b.Configuration;
        }

        public static Task ConfigureEventDrivenThinking(this IServiceProvider provider)
        {
            return provider.GetRequiredService<IBootstrapper>().Configure(provider);
        }
    }
}