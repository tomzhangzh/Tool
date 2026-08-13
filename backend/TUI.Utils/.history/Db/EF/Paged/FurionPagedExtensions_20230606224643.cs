// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Core;

/// <summary>
/// Furion 分页扩展
/// </summary>
public static class FurionPagedExtensions
{
    ///// <summary>
    ///// 分页扩展(返回支持layui格式的数据)
    ///// </summary>
    ///// <typeparam name="TEntity"></typeparam>
    ///// <param name="entities"></param>
    ///// <param name="pageIndex">查询页数</param>
    ///// <param name="pageSize">条数</param>
    ///// <returns></returns>
    //public static LayuiPagedList<TEntity> ToPagedListToLayui<TEntity>(this IQueryable<TEntity> entities, long pageIndex = 1, long pageSize = 20)
    //{
    //    var totalCount = entities.Count();
    //    var items = entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
    //    var totalPages = (long)Math.Ceiling(totalCount / (double)pageSize);

    //    return new LayuiPagedList<TEntity>
    //    {
    //        page = pageIndex,
    //        limit = pageSize,
    //        data = items,
    //        count = totalCount,
    //        totalpage = totalPages
    //    };
    //}

    ///// <summary>
    ///// 分页扩展(返回支持layui格式的数据)
    ///// </summary>
    ///// <typeparam name="TEntity"></typeparam>
    ///// <param name="entities"></param>
    ///// <param name="pageIndex">查询页数</param>
    ///// <param name="pageSize">条数</param>
    ///// <param name="cancellationToken"></param>
    ///// <returns></returns>
    //public static async Task<LayuiPagedList<TEntity>> ToLayuiPagedListAsync<TEntity>(this IQueryable<TEntity> entities, long pageIndex = 1, long pageSize = 20, CancellationToken cancellationToken = default)
    //{
    //    var totalCount = await entities.CountAsync(cancellationToken);
    //    var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    //    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    //    return new LayuiPagedList<TEntity>
    //    {
    //        page = pageIndex,
    //        limit = pageSize,
    //        data = items,
    //        count = totalCount,
    //        totalpage = totalPages
    //    };
    //}

    #region furion

    /// <summary>
    /// 分页扩展
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">索引</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<TEntity> ToPagedList<TEntity>(this IList<TEntity> entities, int pageIndex = 1, int pageSize = 20)
       where TEntity : new()
    {
        var totalCount = entities.Count;
        var items = entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页扩展
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">索引</param>
    /// <param name="pageSize">条数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<PagedList<TEntity>> ToPagedListAsync<TEntity>(this IList<TEntity> entities, int pageIndex = 1, int pageSize = 20, CancellationToken cancellationToken = default)
       where TEntity : new()
    {
        var totalCount = await entities.CountAsync(cancellationToken);
        var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    #endregion furion
}