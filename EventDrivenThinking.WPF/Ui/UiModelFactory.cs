using System;
using EventDrivenThinking.EventInference.Projections;

namespace EventDrivenThinking.Ui
{
    public class UiModelFactory : IModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UiModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TModel Create<TModel>()
        {
            return ViewModelFactory<TModel>.Create(_serviceProvider);
        }
    }
}