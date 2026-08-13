// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.Caching.Distributed;
using SqlSugar;

namespace Abc.Utils;

/// <summary>
/// 依赖缓存扩展
/// </summary>
public static class Caches
{
    /// <summary>
    /// 缓存前缀
    /// </summary>
    public static string CachePrefix { get; } = "ABC_Cache_";

    /// 获取缓存key
    /// </summary>
    /// <typeparam name="TEntity">实体名称</typeparam>
    /// <returns></returns>
    public static string GetTableCacheKey<TEntity>()
    {
        var entityName = typeof(TEntity).Name;
        var cachekey = $"{CachePrefix}Table_{entityName}";
        return cachekey;
    }

    /// <summary>
    /// 获取缓存key
    /// </summary>
    /// <typeparam name="TEntity">实体名称</typeparam>
    /// <returns></returns>
    public static string GetTableCacheKey(string dbTableName)
    {
        var cachekey = $"{CachePrefix}Table_{dbTableName}";
        return cachekey;
    }

    /// <summary>
    /// 获取缓存key
    /// </summary>
    /// <typeparam name="key">缓存的Key</typeparam>
    /// <returns></returns>
    public static string GetCacheKey(string key)
    {
        return $"{CachePrefix}{key}";
    }

    #region 同步设置缓存

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="slidingExpirationSeconds">缓存秒数，滑动过期秒数</param>
    /// <param name="absoluteExpirationSeconds">缓存秒数，绝对过期秒数</param>
    public static void Set(string cachekey, object obj, int slidingExpirationSeconds, int absoluteExpirationSeconds = 0)
    {
        var options = GetDefaultDistributedCacheEntryOptions();
        //设置滑动过期时间
        if (slidingExpirationSeconds > 0)
        {
            options.SlidingExpiration = new TimeSpan(0, 0, slidingExpirationSeconds);
        }
        //设置绝对过期时间
        if (absoluteExpirationSeconds > 0)
        {
            options.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(absoluteExpirationSeconds);
        }

        //如果都没有设置，表示长期缓存，不过期。。。除非手动操作

        Set(cachekey, obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="slidingExpiration">设置缓存项在被删除之前可以处于非活动状态的时间，生存期不会延长到绝对过期</param>
    /// <param name="absoluteExpirationRelativeToNow">获取或设置相对于现在的绝对过期时间</param>
    /// <param name="absoluteExpiration">获取或设置缓存项的绝对过期日期</param>
    public static void Set(string cachekey, object obj, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpirationRelativeToNow = null, DateTimeOffset? absoluteExpiration = null)
    {
        var options = GetDefaultDistributedCacheEntryOptions();
        //设置滑动过期时间
        if (slidingExpiration != null)
        {
            options.SlidingExpiration = slidingExpiration;
        }
        //设置绝对过期时间
        if (absoluteExpirationRelativeToNow != null)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        }
        //设置绝对过期时间
        if (absoluteExpiration != null)
        {
            options.AbsoluteExpiration = absoluteExpiration;
        }

        //如果都没有设置，表示长期缓存，不过期。。。除非手动操作

        Set(cachekey, obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="options">缓存选项</param>
    public static void Set(string cachekey, object obj, DistributedCacheEntryOptions? options)
    {
        var _cache = App.GetService<IDistributedCache>();
        if (options == null) options = GetDefaultDistributedCacheEntryOptions();
        var jsonstring = obj.ToJson();
        _cache.SetString(cachekey, jsonstring, options);
        SetCacheKey(cachekey);
    }

    #endregion 同步设置缓存

    #region 异步设置缓存

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="slidingExpirationSeconds">缓存秒数，滑动过期秒数</param>
    /// <param name="absoluteExpirationSeconds">缓存秒数，绝对过期秒数</param>
    public static async Task SetAsync(string cachekey, object obj, int slidingExpirationSeconds, int absoluteExpirationSeconds = 0, CancellationToken token = default)
    {
        var options = GetDefaultDistributedCacheEntryOptions();
        //设置滑动过期时间
        if (slidingExpirationSeconds > 0)
        {
            options.SlidingExpiration = new TimeSpan(0, 0, slidingExpirationSeconds);
        }
        //设置绝对过期时间
        if (absoluteExpirationSeconds > 0)
        {
            options.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(absoluteExpirationSeconds);
        }

        //如果都没有设置，表示长期缓存，不过期。。。除非手动操作
        await SetAsync(cachekey, obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="slidingExpiration">设置缓存项在被删除之前可以处于非活动状态的时间，生存期不会延长到绝对过期</param>
    /// <param name="absoluteExpirationRelativeToNow">获取或设置相对于现在的绝对过期时间</param>
    /// <param name="absoluteExpiration">获取或设置缓存项的绝对过期日期</param>
    public static async Task SetAsync(string cachekey, object obj, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpirationRelativeToNow = null, DateTimeOffset? absoluteExpiration = null, CancellationToken token = default)
    {
        var options = GetDefaultDistributedCacheEntryOptions();
        //设置滑动过期时间
        if (slidingExpiration != null)
        {
            options.SlidingExpiration = slidingExpiration;
        }
        //设置绝对过期时间
        if (absoluteExpirationRelativeToNow != null)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        }
        //设置绝对过期时间
        if (absoluteExpiration != null)
        {
            options.AbsoluteExpiration = absoluteExpiration;
        }

        //如果都没有设置，表示长期缓存，不过期。。。除非手动操作
        await SetAsync(cachekey, obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    /// <param name="obj">对象</param>
    /// <param name="options">缓存选项</param>
    public static async Task SetAsync(string cachekey, object obj, DistributedCacheEntryOptions? options, CancellationToken token = default)
    {
        var _cache = App.GetService<IDistributedCache>();
        if (options == null) options = GetDefaultDistributedCacheEntryOptions();
        var jsonstring = obj.ToJson();
        await _cache.SetStringAsync(cachekey, jsonstring, options, token);
        SetCacheKey(cachekey);
    }

    #endregion 异步设置缓存

    #region 获取缓存

    /// <summary>
    /// 获取缓存数据
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="cacheKey">缓存Key</param>
    /// <returns>返回的实体对象</returns>
    public static TEntity? Get<TEntity>(string cacheKey)
    {
        var _cache = App.GetService<IDistributedCache>();
        var databytes = _cache.GetString(cacheKey);
        if (string.IsNullOrWhiteSpace(databytes))
        {
            return default;
        }
        var entities = databytes.ToObj<TEntity>();
        return entities;
    }

    /// <summary>
    /// 获取缓存数据
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="cacheKey">缓存Key</param>
    /// <returns>返回的实体对象</returns>
    public static async Task<TEntity?> GetAsync<TEntity>(string cacheKey, CancellationToken token = default)
    {
        var _cache = App.GetService<IDistributedCache>();
        var databytes = await _cache.GetStringAsync(cacheKey, token);
        if (string.IsNullOrWhiteSpace(databytes))
        {
            return default;
        }
        var entities = databytes.ToObj<TEntity>();
        return entities;
    }

    #endregion 获取缓存

    #region 移除缓存

    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <param name="cacheKey">缓存Key</param>
    public static void Remove(string cacheKey)
    {
        var _cache = App.GetService<IDistributedCache>();
        _cache.Remove(cacheKey);
        RemoveCacheKey(cacheKey);
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <param name="cacheKey">缓存Key</param>
    /// <param name="token"></param>
    public static async Task RemoveAsync(string cacheKey, CancellationToken token = default)
    {
        var _cache = App.GetService<IDistributedCache>();
        await _cache.RemoveAsync(cacheKey, token);
        RemoveCacheKey(cacheKey);
    }
    /// <summary>
    /// 移除缓存Key,根据缓存前缀
    /// </summary>
    /// <param name="prefix">缓存前缀</param>
    public static void RemoveByPrefix(string prefix)
    {
        var cacheKeys = GetAllCacheKeys(prefix);
        var _cache = App.GetService<IDistributedCache>();
        foreach (var cacheKey in cacheKeys)
        {
            _cache.Remove(cacheKey);
        }
        RemoveCacheKey(cacheKeys);
    }
    /// <summary>
    /// 移除缓存Key,根据缓存前缀
    /// </summary>
    /// <param name="prefix">缓存前缀</param>
    /// <param name="token"></param>
    public static async Task RemoveByPrefixAsync(string prefix, CancellationToken token = default)
    {
        var cacheKeys = GetAllCacheKeys(prefix);
        var _cache = App.GetService<IDistributedCache>();
        foreach (var cacheKey in cacheKeys)
        {
            await _cache.RemoveAsync(cacheKey, token);
        }
        RemoveCacheKey(cacheKeys);
    }
    #endregion 移除缓存

    #region 刷新缓存 滑动过期时间

    /// <summary>
    /// 刷新缓存(重置滑动过期时间)
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    public static void Refresh(string cachekey)
    {
        var _cache = App.GetService<IDistributedCache>();
        _cache.Refresh(cachekey);
        SetCacheKey(cachekey);
    }

    /// <summary>
    /// 刷新缓存(重置滑动过期时间)
    /// </summary>
    /// <param name="cachekey">缓存Key</param>
    public static async Task RefreshAsync(string cachekey, CancellationToken token = default)
    {
        var _cache = App.GetService<IDistributedCache>();
        await _cache.RefreshAsync(cachekey, token);
        SetCacheKey(cachekey);
    }

    #endregion 刷新缓存 滑动过期时间

    #region 判断缓存是否存在

    /// <summary>
    /// 判断缓存是否存在
    /// </summary>
    /// <param name="cacheKey">缓存Key</param>
    /// <returns>返回的bool</returns>
    public static bool Exist(string cacheKey)
    {
        var _cache = App.GetService<IDistributedCache>();
        var data = _cache.GetString(cacheKey);
        if (data == null) return false;
        return true;
    }

    /// <summary>
    /// 判断缓存是否存在
    /// </summary>
    /// <param name="cacheKey">缓存Key</param>
    /// <returns>返回的bool</returns>
    public static async Task<bool> ExistAsync(string cacheKey, CancellationToken token = default)
    {
        var _cache = App.GetService<IDistributedCache>();
        var data = await _cache.GetStringAsync(cacheKey, token);
        if (data == null) return false;
        return true;
    }

    #endregion 判断缓存是否存在

    /// <summary>
    /// 获取默认内存缓存选项
    /// </summary>
    /// <returns></returns>
    private static DistributedCacheEntryOptions GetDefaultDistributedCacheEntryOptions()
    {
        var options = new DistributedCacheEntryOptions()
        {
            //SlidingExpiration = new System.TimeSpan(1, 1, 1, 1),
            //AbsoluteExpirationRelativeToNow= new System.TimeSpan(1, 1, 1, 1),
            //AbsoluteExpiration=DateTimeOffset.Now.AddDays(10)
        };

        return options;
    }

    #region 缓存Key

    //主要用于特殊情况，比如sqlsugar的缓存继承封装需要用到所有的缓存keys，但是依赖缓存又没有提供此方法接口获取

    /// <summary>
    /// 设置缓存Key
    /// </summary>
    /// <param name="cacheKey"></param>
    private static void SetCacheKey(string cacheKey)
    {
        var cacheKeys = GetAllDicCacheKeys();
        if (cacheKeys.ContainsKey(cacheKey)) return;
        cacheKeys.TryAdd(cacheKey, cacheKey);
        Set("System_Default_CacheKeys", cacheKeys.Keys.ToList());
    }

    /// <summary>
    /// 移除缓存Key
    /// </summary>
    /// <param name="cacheKey"></param>
    private static void RemoveCacheKey(string cacheKey)
    {
        var cacheKeys = GetAllDicCacheKeys();
        if (cacheKeys.ContainsKey(cacheKey))
        {
            cacheKeys.TryRemove(cacheKey, out _);
        }

        Set("System_Default_CacheKeys", cacheKeys.Keys.ToList());
    }
    /// <summary>
    /// 移除缓存Key
    /// </summary>
    /// <param name="cacheKey"></param>
    private static void RemoveCacheKey(List<string> removeCacheKeys)
    {
        var cacheKeys = GetAllDicCacheKeys();
        foreach (var cacheKey in removeCacheKeys)
        {
            if (cacheKeys.ContainsKey(cacheKey))
            {
                cacheKeys.TryRemove(cacheKey, out _);
            }
        }
        Set("System_Default_CacheKeys", cacheKeys.Keys.ToList());
    }


    /// <summary>
    /// 获取线程安全的缓存Key集合
    /// </summary>
    /// <returns></returns>
    private static ConcurrentDictionary<string, string> GetAllDicCacheKeys()
    {
        var cacheKeys = GetAllCacheKeys();
        if (cacheKeys == null) cacheKeys = new List<string>();
        var dic = new ConcurrentDictionary<string, string>();
        foreach (var key in cacheKeys)
        {
            dic.TryAdd(key, key);
        }
        return dic;
    }

    /// <summary>
    ///  /// <summary>
    /// 获取所有的缓存Key【实际上可能并不是全部，因为部分地方可能并没有使用Caches类来操作缓存，这部分没有获取到】
    /// </summary>
    /// </summary>
    /// <param name="prefix">前缀，默认为空，返回全部</param>
    /// <returns></returns>
    public static List<string> GetAllCacheKeys(string prefix = "")
    {
        var cacheKeys = Get<List<string>>("System_Default_CacheKeys");
        if (cacheKeys == null) return new List<string>();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return cacheKeys;
        }
        else
        {
            return cacheKeys.Where(o => o.StartsWith(prefix)).ToList();
        }
    }

    #endregion 缓存Key
}