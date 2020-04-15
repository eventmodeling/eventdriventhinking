using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public interface ISubscription
    {
        Task Catchup();
        ISubscription Merge(ISubscription single);
    }
}