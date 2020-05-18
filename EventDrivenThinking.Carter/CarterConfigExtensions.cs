using System;
using System.Linq;
using Carter;
using EventDrivenThinking.App.Configuration;

namespace EventDrivenThinking.Carter
{
    public static class CarterConfigExtensions
    {
        public static Action<CarterConfigurator> GetCarterConfigurator(this Configuration config)
        {
            var factory = config.Services.ResolveExtension<CarterModuleFactory>();
            var modules = factory.GetModules().ToArray();
            return config => config.WithModules(modules);
        }

        public static AggregateConfig BindCarter(this AggregateConfig config)
        {
            config.Merge(new CarterComandHandlerSliceStartup());
            return config;
        }

    }
}