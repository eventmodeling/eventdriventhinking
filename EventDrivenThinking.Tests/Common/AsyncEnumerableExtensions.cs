using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventDrivenThinking.Tests.Common
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<T> LastOrDefault<T>(this IAsyncEnumerable<T> collection)
        {
            T lastOrDefault = default(T);
            await foreach (var i in collection)
            {
                lastOrDefault = i;
            }

            return lastOrDefault;
        }
        public static async Task<T> FirstOrDefault<T>(this IAsyncEnumerable<T> collection)
        {
            T firstOrDefault = default(T);
            await foreach (var i in collection)
            {
                firstOrDefault = i;
                break;
            }

            return firstOrDefault;
        }
    }
}