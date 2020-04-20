using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Projections
{
    /// <summary>
    /// Used to create instances of items of models. Be aware, root model-object should be created by container.
    /// </summary>
    public interface IModelFactory
    {
        TModel Create<TModel>();
    }
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