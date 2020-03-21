namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQuery { }
    /// <summary>
    /// Query is specific when it comes to result and source model.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public interface IQuery<TModel, TResult> : IQuery 
        where TModel:IModel
    {

    }
}