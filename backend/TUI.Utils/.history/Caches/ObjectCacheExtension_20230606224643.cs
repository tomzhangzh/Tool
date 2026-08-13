// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.Caching.Distributed;

namespace TUI.Utils;

/// <summary>
/// Table 缓存扩展
/// </summary>
public static class ObjectCacheExtension
{
    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="cacheKey">缓存Key</param>
    /// <param name="options">缓存选项</param>
    public static void SetCache(this object obj, string cacheKey, DistributedCacheEntryOptions? options = null)
    {
        Caches.Set(Caches.GetCacheKey(cacheKey), obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="cacheKey">缓存Key</param>
    /// <param name="options">缓存选项</param>
    public static async Task SetCacheAsync(this object obj, string cacheKey, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        await Caches.SetAsync(Caches.GetCacheKey(cacheKey), obj, options, token);
    }
}