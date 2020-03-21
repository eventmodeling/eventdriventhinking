namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    /// <summary>
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public interface IQueryHandler<in TQuery, in TModel, out TResult>
    {
        TResult Execute(TModel model, TQuery query);
    }
}