using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Serilog.ILogger;

#pragma warning disable 1998

namespace EventDrivenThinking.App.Configuration.Server
{
    public static class SubscribePipeExtensions
    {
        public static void WithEventStore(this SubscribePipe pipe)
        {
            
        }

    }
   
}