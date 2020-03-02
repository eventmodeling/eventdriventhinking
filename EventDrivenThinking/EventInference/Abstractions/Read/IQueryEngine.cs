using System;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryEngine<TModel> 
        where TModel : IModel
    {
        TModel CreateOrGet();
        void Subscribe<TQuery, TResult>(TQuery query, Action<TResult> onComplete) where TQuery : IQuery<TModel, TResult>;
        //TODO: Unscubscibe and load
    }
}