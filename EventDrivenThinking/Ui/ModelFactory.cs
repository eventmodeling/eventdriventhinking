using System;
using EventDrivenThinking.EventInference.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.Ui
{
    public class ModelFactory : IModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TModel Create<TModel>()
        {
            return ActivatorUtilities.GetServiceOrCreateInstance<TModel>(_serviceProvider);
        }
    }
}