using System;
using EventDrivenThinking.App.Configuration.Fresh;
using EventDrivenThinking.App.Configuration.Fresh.EventStore;
using EventDrivenThinking.App.Configuration.Fresh.Http;
using EventDrivenThinking.Integrations.Unity;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Unity;
using Xunit;

namespace EventDrivenUi.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void Config()
        {
            UnityContainer c = new UnityContainer();
            IServiceCollection collection = new UnityServiceCollection(c);
            IServiceProvider serviceProvider = new UnityServiceProvider(c);
            
            collection.AddEventDrivenThinking(Logger.None,x =>
            {
                x.AddAssemblies(); // optional

                EventStoreConfigExtensions.SubscribeFromEventStore(EventStoreConfigExtensions.SubscribeFromEventStore(x.Slices.SelectByTag("NameSpace")
                            .Aggregates.WriteToEventStore()
                            .Projections)
                        .Processors)
                    .CommandInvocations.UseHttp();
            });
            
            serviceProvider.ConfigureEventDrivenThinking();
        }
    }
}