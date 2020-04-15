using System.Threading.Tasks;
using Nito.AsyncEx;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    class Subscription : ISubscription
    {
        private readonly AsyncManualResetEvent _catchup;

        public Subscription(bool isLive = false)
        {
            _catchup = new AsyncManualResetEvent(isLive);
        }

        public void MakeLive()
        {
            _catchup.Set();
        }
        public Task Catchup()
        {
            //_catchup.Wait();
            return _catchup.WaitAsync();
            //return Task.CompletedTask;
        }

        public ISubscription Merge(ISubscription single)
        {
            if (single is MultiSubscription)
                return single.Merge(this);
            else return new MultiSubscription(this, single);
        }
    }
}