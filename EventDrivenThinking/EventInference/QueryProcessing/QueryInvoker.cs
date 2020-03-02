using System;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    /// <summary>
    /// Understands how to return results. Uses IUiEventBus to invoke queries and subscribe for results.
    /// </summary>
    public class QueryInvoker : IQueryInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MethodInfo _executeGet;
        public QueryInvoker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _executeGet = this.GetType().GetMethod(nameof(ExecuteGet), BindingFlags.NonPublic | BindingFlags.Instance);
        }


        public IQueryResult<TModel, TResult> Get<TModel, TResult>(IQuery<TModel, TResult> query, QueryOptions options = null)
            where TModel : IModel
        {
            var queryType = query.GetType();
            return (IQueryResult<TModel, TResult>)_executeGet
                .MakeGenericMethod(queryType, typeof(TModel), typeof(TResult))
                .Invoke(this, new object[] { query, options });
        }
        IQueryResult<TModel, TResult> ExecuteGet<TQuery, TModel, TResult>(TQuery query, QueryOptions options = null)
            where TModel : IModel
            where TQuery : IQuery<TModel, TResult>
        {
            var engine = _serviceProvider.GetService<IQueryEngine<TModel>>();

            var model = engine.CreateOrGet();
            var queryResult = new QueryResult<TQuery, TModel, TResult>(model, engine, query, options);

            engine.Subscribe<TQuery, TResult>(query, result => queryResult.OnComplete(result));


            return queryResult;
        }
    }
}