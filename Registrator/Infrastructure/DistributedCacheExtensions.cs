using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MagazinchikAPI.Infrastructure
{
    public static class DistributedCacheExtensions
    {
        private readonly static TimeSpan defaultExpireTime = TimeSpan.FromMinutes(3);
        public static async Task SetRecordAsync<T>
        (this IDistributedCache cache, string recordId, T record,
        TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? defaultExpireTime,
                SlidingExpiration = unusedExpireTime
            };

            var jsonRecord = JsonSerializer.Serialize(record);
            await cache.SetStringAsync(recordId, jsonRecord, options);
        }

        public static async Task<T> GetRecordAsync<T>(this IDistributedCache cache, string recordId)
        {
            var jsonRecord = await cache.GetStringAsync(recordId);

            if (jsonRecord is null) return default!;

            return JsonSerializer.Deserialize<T>(jsonRecord)!;
        }
    }
}