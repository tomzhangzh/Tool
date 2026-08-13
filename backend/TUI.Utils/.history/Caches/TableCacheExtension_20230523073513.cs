// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.Caching.Distributed;

namespace Abc.Utils;

/// <summary>
/// Table 缓存扩展
/// </summary>
public static class TableCacheExtension
{
    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="cacheKey">缓存Key</param>
    /// <param name="options">缓存选项</param>
    public static void SetTableCache(this object obj, string cacheKey, DistributedCacheEntryOptions? options = null)
    {
        Caches.Set(cacheKey, obj, options);
    }

    /// <summary>
    /// 设置缓存，缓存key默认为： 缓存前缀+ Table_ + 表名
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="options">缓存选项</param>
    public static void SetTableCache<TEntity>(this object obj, DistributedCacheEntryOptions? options = null)
    {
        Caches.Set(Caches.GetTableCacheKey<TEntity>(), obj, options);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="cacheKey">缓存Key</param>
    /// <param name="options">缓存选项</param>
    public static async Task SetTableCacheAsync(this object obj, string cacheKey, DistributedCacheEntryOptions options = null, CancellationToken token = default)
    {
        await Caches.SetAsync(cacheKey, obj, options, token);
    }

    /// <summary>
    /// 设置缓存，缓存key默认为： 缓存前缀+ Table_ + 表名
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="options">缓存选项</param>
    public static async Task SetTableCacheAsync<TEntity>(this object obj, DistributedCacheEntryOptions options = null, CancellationToken token = default)
    {
        await Caches.SetAsync(Caches.GetTableCacheKey<TEntity>(), obj, options, token);
    }
}