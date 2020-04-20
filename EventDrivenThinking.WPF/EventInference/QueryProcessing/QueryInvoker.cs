using System;
using System.Reflection;
using System.Threading.Tasks;
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


        public Task<ILiveResult<TResult>> Get<TModel, TResult>(IQuery<TModel, TResult> query, QueryOptions options = null)
            where TModel : IModel
        {
            var queryType = query.GetType();
            return (Task< ILiveResult<TResult>>)_executeGet
                .MakeGenericMethod(queryType, typeof(TModel), typeof(TResult))
                .Invoke(this, new object[] { query, options });
        }
        Task<ILiveResult<TResult>> ExecuteGet<TQuery, TModel, TResult>(TQuery query, QueryOptions options = null)
            where TModel : IModel
            where TQuery : IQuery<TModel, TResult>
            where TResult : class
        {
            var engine = _serviceProvider.GetService<IQueryEngine<TModel>>();
            return engine.Execute<TQuery,TResult>(query, options);
        }
    }
}