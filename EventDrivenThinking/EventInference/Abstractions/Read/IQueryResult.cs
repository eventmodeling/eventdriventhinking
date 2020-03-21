using System;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryResult : IDisposable
    {
        object Result { get; }
        object Model { get; }
        event EventHandler Completed;
    }
    /// <summary>
    /// Dispose causes to result to unsubscribe.
    /// QueryResult can be same for many models and results.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IQueryResult<out TModel, out TResult> : IQueryResult
        where TModel:IModel
    {
        new TResult Result { get; }
        new TModel Model { get; }
    }
}