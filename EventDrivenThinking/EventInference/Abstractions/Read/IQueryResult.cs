using System;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    /// <summary>
    /// Dispose causes to result to unsubscribe.
    /// QueryResult can be same for many models and results.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IQueryResult<out TModel, out TResult> : IDisposable
        where TModel:IModel
    {
        TResult Result { get; }
        TModel Model { get; }
        /// <summary>
        /// Completed is fired when first snapshot is loaded into the model or if all old events where applied.
        /// </summary>
        event EventHandler Completed;
    }
}