using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private IProjectionSchema[] _projections;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddSingleton<IModelProjectionSubscriber<>, EventAggregatorModelProjectionSubscriber>();
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            await ActivatorUtilities.CreateInstance<EventAggregatorSubscriber>(serviceProvider)
                .Subscribe(_projections.SelectMany(x=>x.Events));
        }

        public void Initialize(IEnumerable<IProjectionSchema> projections)
        {
            this._projections = projections.ToArray();
        }
    }
}