using System.Threading.Tasks;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface ISubscriptionProvider<TOwnerInterface>
    {
        bool CanMerge(ISubscriptionProvider<TOwnerInterface> other);
        ISubscriptionProvider<TOwnerInterface> Merge(ISubscriptionProvider<TOwnerInterface> other);
        Task Subscribe(ISchema schema, IEventHandlerFactory factory, object[] args = null);
    }
}