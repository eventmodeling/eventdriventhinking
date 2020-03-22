using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public interface IQueryInvoker
    {
        Task<ILiveResult<TResult>> Get<TModel, TResult>(IQuery<TModel, TResult> query, QueryOptions options = null)
            where TModel : IModel;
    }
}