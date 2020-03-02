﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Integrations.EventAggregator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.Fresh.EventAggregator
{
    public class ProjectionsSliceStartup : IProjectionSliceStartup
    {
        private IProjectionSchema[] _projections;

        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISubscriptionManager, EventAggregatorSubscriptionManager>();
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