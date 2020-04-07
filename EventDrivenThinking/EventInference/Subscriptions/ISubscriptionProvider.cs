using System.ComponentModel;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface ISubscriptionProvider<TOwnerInterface, TSchema>
    where TSchema : ISchema
    {
        void Init(TSchema schema);
        bool CanMerge(ISubscriptionProvider<TOwnerInterface, TSchema> other);
        ISubscriptionProvider<TOwnerInterface, TSchema> Merge(ISubscriptionProvider<TOwnerInterface, TSchema> other);
        Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null);
    }
}