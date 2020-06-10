using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.Ui
{
    public static class DispatcherQueueExtensions
    {
        public static Task CompleteOnUi<T>(this Task<T> task,  Func<T, Task> onComplete)
        {
            return task.ContinueWith(x =>
            {
                DispatcherQueue.Instance.Enqueue(() => onComplete(x.Result));
            });
        }

        public static void UpdatesUi<T>(this Task<ILiveResult<T>> task, Action<T> onUpdate)
        {
            task.ContinueWith(x =>
            {
                void OnUpdateUi(object s, EventArgs a)
                {
                    DispatcherQueue.Instance.Enqueue(() => onUpdate(x.Result.Result));
                        x.Result.ResultUpdated -= OnUpdateUi;
                }

                x.Result.ResultUpdated += OnUpdateUi;
            });
        }
        public static Task CompleteOnUi<T>(this Task<T> task, Action<T> onComplete)
        {
            return task.ContinueWith(x =>
            {
                DispatcherQueue.Instance.Enqueue(() => onComplete(x.Result));
            });
        }
    }
    public class DispatcherQueue
    {
        public static readonly DispatcherQueue Instance = new DispatcherQueue();
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ConcurrentQueue<Action> _queue;
        private int _queued;

        private DispatcherQueue()
        {
            _synchronizationContext = SynchronizationContext.Current;
            if(_synchronizationContext == null)
                _synchronizationContext = new SynchronizationContext();
            _queue = new ConcurrentQueue<Action>();
        }
        public void Enqueue(Func<Task> task)
        {
            Enqueue(() => task().GetAwaiter().GetResult());
        }
        public void Enqueue(Action task)
        {
            _queue.Enqueue(task);
            if (Interlocked.Increment(ref _queued) == 1)
            {
                _synchronizationContext.Post(ProcessQueue, null); // Async on purpose.
            }
        }

        private void ProcessQueue(object? state)
        {
            while (_queue.TryDequeue(out Action d))
            {
                d();
                Interlocked.Decrement(ref _queued);
            }
        }
    }
}
