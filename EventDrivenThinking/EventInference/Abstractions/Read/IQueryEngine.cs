using System;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryEngine<TModel> 
        where TModel : IModel
    {
        IQueryResult<TModel, TResult> Execute<TQuery, TResult>(TQuery query, QueryOptions options = null)
            where TQuery : IQuery<TModel, TResult>;
    }
}