using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace IBS.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            TimeSpan absoluteExpiration,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    }

    public sealed class MemoryCacheService(IMemoryCache cache): ICacheService
    {
        private static readonly ConcurrentDictionary<string, byte> _keys = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                cache.TryGetValue(key, out T? value) ? value : default
            );
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            TimeSpan absoluteExpiration,
            CancellationToken cancellationToken = default)
        {
            cache.Set(
                key,
                value,
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = slidingExpiration,
                    AbsoluteExpirationRelativeToNow = absoluteExpiration,
                    Size = 1,
                    PostEvictionCallbacks =
                    {
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = (_, _, _, _) => { _keys.TryRemove(key, out var _); }
                        }
                    }
                });

            _keys.TryAdd(key, 0);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            foreach (var key in _keys.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    cache.Remove(key);
                    _keys.TryRemove(key, out _);
                }
            }

            return Task.CompletedTask;
        }

    }
}
