using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.Carter;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Carter
{
    public class CarterComandHandlerSliceStartup : IAggregateSliceStartup, IServiceExtensionConfigProvider
    {
        private IAggregateSchema[] _aggregates;
        private CarterModuleFactory _factory;

        public void RegisterServices(IServiceCollection collection)
        {
            this._factory = new CarterModuleFactory(_aggregates);
            collection.AddSingleton(_factory);
            
        }


        public Task ConfigureServices(IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public void Initialize(IEnumerable<IAggregateSchema> aggregates)
        {
            this._aggregates = aggregates.ToArray();
        }

        public void Register(IServiceExtensionProvider services)
        {
            services.AddExtension<CarterModuleFactory>(_factory);
        }
    }
    
}
