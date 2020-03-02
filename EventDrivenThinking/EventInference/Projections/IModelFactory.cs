using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Projections
{
    /// <summary>
    /// Used to create instances of items of models. Be aware, root model-object should be created by container.
    /// </summary>
    public interface IModelFactory
    {
        TModel Create<TModel>();
    }

}