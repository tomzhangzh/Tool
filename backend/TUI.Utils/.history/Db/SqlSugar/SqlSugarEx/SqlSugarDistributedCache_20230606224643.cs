// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// sqlSugar 依赖缓存，底层基于 IDistributedCache
/// </summary>
public class SqlSugarDistributedCache : ICacheService
{
    /// <summary>
    /// 添加缓存
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add<V>(string key, V value)
    {
        Caches.Set(key, value);
    }

    /// <summary>
    /// 添加缓存
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="cacheDurationInSeconds">缓存过期秒数</param>
    public void Add<V>(string key, V value, int cacheDurationInSeconds)
    {
        //限制最大值防止无止境的存储
        //限制为最长2个小时
        if (cacheDurationInSeconds > (7200))
        {
            cacheDurationInSeconds = 3600 + RandomHelper.GetInt(100, 999);
        }

        Caches.Set(key, value, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions() { SlidingExpiration = new TimeSpan(0, 0, cacheDurationInSeconds) });
    }

    /// <summary>
    /// 是否包含某个缓存Key
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainsKey<V>(string key)
    {
        return Caches.Exist(key);
    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public V Get<V>(string key)
    {
        return Caches.Get<V>(key);
    }

    /// <summary>
    /// 获取所有缓存Key
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <returns></returns>
    public IEnumerable<string> GetAllKey<V>()
    {
        return Caches.GetAllCacheKeys("SqlSugarDataCache");
    }

    /// <summary>
    /// 获取或者创建
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="cacheKey"></param>
    /// <param name="create"></param>
    /// <param name="cacheDurationInSeconds"></param>
    /// <returns></returns>
    public V GetOrCreate<V>(string cacheKey, Func<V> create, int cacheDurationInSeconds = int.MaxValue)
    {
        if (this.ContainsKey<V>(cacheKey))
        {
            var result = this.Get<V>(cacheKey);
            if (result == null)
            {
                return create();
            }
            else
            {
                return result;
            }
        }
        else
        {
            var result = create();
            this.Add(cacheKey, result, cacheDurationInSeconds);
            return result;
        }
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="key"></param>
    public void Remove<V>(string key)
    {
        Caches.Remove(key);
    }
}