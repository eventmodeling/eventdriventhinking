using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface ISubscriptionProvider<TOwnerInterface, TSchema>
    where TSchema : ISchema
    {
        bool CanMerge(ISubscriptionProvider<TOwnerInterface, TSchema> other);
        ISubscriptionProvider<TOwnerInterface, TSchema> Merge(ISubscriptionProvider<TOwnerInterface, TSchema> other);
        Task Subscribe(TSchema schema, IEventHandlerFactory factory, object[] args = null);
    }
}