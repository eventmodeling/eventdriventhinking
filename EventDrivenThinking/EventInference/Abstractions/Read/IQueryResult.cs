using System;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface ILiveResult : IDisposable
    {
        LiveQueryStatus Status { get; }
        object Result { get; }
        event EventHandler ResultUpdated;
        event EventHandler StatusChanged;
    }

    public enum LiveQueryStatus
    {
        Initialized,
        Running,
        Disposed
    }
    /// <summary>
    /// Dispose causes to result to unsubscribe.
    /// QueryResult can be same for many models and results.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface ILiveResult<out TResult> : ILiveResult
    {
        new TResult Result { get; }
    }
}