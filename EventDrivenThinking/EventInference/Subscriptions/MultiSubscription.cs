using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    class MultiSubscription : ISubscription
    {
        private readonly List<ISubscription> _subscriptions;

        public MultiSubscription(params ISubscription[] subscriptions)
        {
            _subscriptions = new List<ISubscription>(subscriptions);
        }
        public ISubscription Merge(ISubscription single)
        {
            _subscriptions.Add(single);
            return this;
        }

        public Task Catchup()
        {
            var s = _subscriptions.Select(x=>x.Catchup()).ToArray();
            return Task.WhenAll(s);
        }
    }
}