using System;
using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryEngine<TModel> 
        where TModel : IModel
    {
        Task<ILiveResult<TResult>> Execute<TQuery, TResult>(TQuery query, QueryOptions options = null)
            where TQuery : IQuery<TModel, TResult>
            where TResult: class; // cannot be struct, because results are live will be changing. 
    }
}