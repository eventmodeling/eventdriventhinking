namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryHandler<in TQuery, in TModel, out TResult>
    {
        TResult Execute(TModel model, TQuery query);
    }
}