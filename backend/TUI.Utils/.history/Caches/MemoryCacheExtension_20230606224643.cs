// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.Caching.Memory;

namespace TUI.Utils;

/// <summary>
/// 内存缓存扩展
/// </summary>
public static class MemoryCacheExtension
{
    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">缓存数据</param>
    /// <param name="cachekey">缓存key</param>
    /// <param name="memoryCacheEntryOptions">缓存选项</param>
    /// <returns></returns>
    public static void SetMemoryCache(this object obj, string cachekey, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
    {
        var _memoryCache = App.GetService<IMemoryCache>();
        if (memoryCacheEntryOptions == null)
            memoryCacheEntryOptions = GetDefaultMemoryCacheEntryOptions();
        _memoryCache.Set(cachekey, obj, memoryCacheEntryOptions);
        // return datas;
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="obj">缓存数据</param>
    /// <param name="cachekey">缓存key</param>
    /// <param name="memoryCacheEntryOptions">缓存选项</param>
    /// <returns></returns>
    public static async Task SetMemoryCacheAsync(this object obj, string cachekey, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
    {
        await Task.Run(() =>
       {
           SetMemoryCache(obj, cachekey, memoryCacheEntryOptions);
       });
    }

    ///// <summary>
    ///// 设置缓存
    ///// </summary>
    ///// <typeparam name="TEntity">TEntity</typeparam>
    ///// <param name="datas">缓存数据</param>
    ///// <param name="cachekey">缓存key</param>
    ///// <param name="memoryCacheEntryOptions">缓存选项</param>
    ///// <returns></returns>
    //public static IEnumerable<TEntity> SetMemoryCache<TEntity>(this IEnumerable<TEntity> datas, string cachekey, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
    //{
    //    var _memoryCache = App.GetService<IMemoryCache>();
    //    if (memoryCacheEntryOptions == null)
    //        memoryCacheEntryOptions = GetDefaultMemoryCacheEntryOptions();
    //    _memoryCache.Set(cachekey, datas, memoryCacheEntryOptions);
    //    return datas;
    //}

    ///// <summary>
    ///// 设置缓存
    ///// </summary>
    ///// <typeparam name="TEntity">TEntity</typeparam>
    ///// <param name="datas">缓存数据</param>
    ///// <param name="cachekey">缓存key</param>
    ///// <param name="memoryCacheEntryOptions">缓存选项</param>
    ///// <returns></returns>
    //public static async Task<IEnumerable<TEntity>> SetMemoryCacheAsync<TEntity>(this IEnumerable<TEntity> datas, string cachekey, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
    //{
    //    return await Task.Run(() =>
    //    {
    //        return SetMemoryCache<TEntity>(datas, cachekey, memoryCacheEntryOptions);
    //    });
    //}

    /// <summary>
    /// 获取内存缓存
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="cachekey">缓存key</param>
    /// <returns></returns>
    public static TEntity GetMemoryCache<TEntity>(string cachekey)
    {
        var _memoryCache = App.GetService<IMemoryCache>();
        var state = _memoryCache.TryGetValue(cachekey, out TEntity datas);
        if (state) return datas;
        return default(TEntity);
    }

    /// <summary>
    /// 获取内存缓存
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="cachekey">缓存key</param>
    /// <returns></returns>
    public static async Task<TEntity> GetMemoryCacheAsync<TEntity>(string cachekey)
    {
        return await Task.Run(() =>
        {
            return GetMemoryCache<TEntity>(cachekey);
        });
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存key</param>
    /// <param name="value">值</param>
    public static void SetMemoryCache(string cachekey, object value)
    {
        var memoryCacheEntryOptions = GetDefaultMemoryCacheEntryOptions();
        SetMemoryCache(cachekey, value, memoryCacheEntryOptions);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存key</param>
    /// <param name="value">值</param>
    public static void SetMemoryCache(string cachekey, object value, MemoryCacheEntryOptions memoryCacheEntryOptions)
    {
        var _memoryCache = App.GetService<IMemoryCache>();
        if (memoryCacheEntryOptions == null)
            memoryCacheEntryOptions = GetDefaultMemoryCacheEntryOptions();
        _memoryCache.Set(cachekey, value, memoryCacheEntryOptions);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存key</param>
    /// <param name="value">值</param>
    public static async Task SetMemoryCacheAsync(string cachekey, object value)
    {
        await Task.Run(() =>
        {
            var memoryCacheEntryOptions = GetDefaultMemoryCacheEntryOptions();
            SetMemoryCache(cachekey, value, memoryCacheEntryOptions);
        });
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="cachekey">缓存key</param>
    /// <param name="value">值</param>
    public static async Task SetMemoryCacheAsync(string cachekey, object value, MemoryCacheEntryOptions memoryCacheEntryOptions)
    {
        await Task.Run(() =>
        {
            SetMemoryCache(cachekey, value, memoryCacheEntryOptions);
        });
    }

    public static List<TEntity> GetMemoryCache<TEntity>(this IQueryable<TEntity> entities, string cachekey)
    {
        return GetMemoryCache<List<TEntity>>(cachekey);
    }

    public static async Task<List<TEntity>> GetMemoryCacheAsync<TEntity>(this IQueryable<TEntity> entities, string cachekey)
    {
        return await GetMemoryCacheAsync<List<TEntity>>(cachekey);
    }

    /// <summary>
    /// 获取默认内存缓存选项
    /// </summary>
    /// <returns></returns>
    public static MemoryCacheEntryOptions GetDefaultMemoryCacheEntryOptions()
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions()
        {
            //SlidingExpiration = new System.TimeSpan(1, 1, 1, 1),
            //AbsoluteExpiration=DateTimeOffset.Now.AddDays(10)
        };

        return memoryCacheEntryOptions;
    }

    /// <summary>
    /// 移除内存缓存
    /// </summary>
    /// <param name="cachekey"></param>
    public static void RemoveMemoryCache(string cachekey)
    {
        var _memoryCache = App.GetService<IMemoryCache>();
        _memoryCache.Remove(cachekey);
    }

    /// <summary>
    /// 移除内存缓存
    /// </summary>
    /// <param name="cachekey"></param>
    /// <returns></returns>
    public static async Task RemoveMemoryCacheAsync(string cachekey)
    {
        await Task.Run(() =>
        {
            RemoveMemoryCache(cachekey);
        });
    }
}