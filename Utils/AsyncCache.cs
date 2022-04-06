using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

// Based on https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/Utils/AsyncCache.cs

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    public class AsyncCache<TKey, TValue>
    {
        private readonly Func<TKey, Task<TValue>> _valueFactory;

        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<TValue>> _completionSourceCache =
            new ConcurrentDictionary<TKey, TaskCompletionSource<TValue>>();

        public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
        {
            this._valueFactory = valueFactory;
        }

        public async Task<TValue> GetItem(TKey key)
        {
            var newSource = new TaskCompletionSource<TValue>();
            var currentSource = _completionSourceCache.GetOrAdd(key, newSource);

            if (currentSource != newSource)
                return await currentSource.Task;

            try
            {
                var result = await _valueFactory(key);
                newSource.SetResult(result);
            }
            catch (Exception e)
            {
                newSource.SetException(e);
            }

            return await newSource.Task;
        }

        public async Task<TValue> RemoveItem(TKey key) => _completionSourceCache.TryRemove(key, out TaskCompletionSource<TValue> item) ? await item.Task : default;
    }
}