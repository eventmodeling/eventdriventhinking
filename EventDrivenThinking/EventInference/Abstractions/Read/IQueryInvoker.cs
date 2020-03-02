namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryInvoker
    {
        IQueryResult<TModel, TResult> Get<TModel, TResult>(IQuery<TModel, TResult> query, QueryOptions options = null)
            where TModel : IModel;
    }
}