using System.Linq;
using EventDrivenThinking.App.Configuration;
using EventDrivenThinking.App.Configuration.Client;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Client;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Ui;
using EventDrivenThinking.Ui.Schema;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Serilog;


namespace EventDrivenThinking.Integrations.EventAggregator.Client
{
    public static class SubscribePipeExtensions
    {
        public static void WithEventAggregator(this SubscribePipe pipe)
        {
            var sp = pipe.ServiceProvider;
            var eventAggregator = sp.GetService<IEventAggregator>();

            var events = pipe.Projections.SelectMany(x => x.Events)
                .Union(pipe.Processors.SelectMany(x => x.Events))
                .Distinct()
                .ToArray();

            //eventAggregator.ConfigureAsEventBusForProjections(events, 
            //    sp.GetService<IEventHandlerDispatcher>(),
            //    sp.GetService<ILogger>());
        }
        
    }
}