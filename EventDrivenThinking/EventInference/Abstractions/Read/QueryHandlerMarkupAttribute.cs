using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Abstractions.Read
{
    public class QueryHandlerMarkupAttribute : MarkupAttribute
    {
        public QueryHandlerMarkupAttribute() : base(typeof(IQueryHandler<,,>))
        {
        }
    }
}