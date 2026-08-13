// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// SqlSugar 分页扩展
/// </summary>
public static class SqlSugarPagedExtensions
{
    /// <summary>
    /// 分页拓展
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static async Task<PagedList<TEntity>> ToPagedListAsync<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        RefAsync<int> totalCount = 0;
        var items = await query.ToOffsetPageAsync(pageIndex, pageSize, totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items,
            TotalCount = (int)totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页拓展
    /// </summary>
    /// <param name="query">ISugarQueryable</param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<TEntity> ToPagedList<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        int totalCount = 0;

        var items = query.ToOffsetPage(pageIndex, pageSize, ref totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items,
            TotalCount = (int)totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页拓展
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static async Task<PagedList<ReturnTentity>> ToPagedListAsync<TEntity, ReturnTentity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        RefAsync<int> totalCount = 0;
        var items = await query.ToOffsetPageAsync(pageIndex, pageSize, totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedList<ReturnTentity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items.Adapt<List<ReturnTentity>>(),
            TotalCount = (int)totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页拓展
    /// </summary>
    /// <param name="query">ISugarQueryable</param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<ReturnTentity> ToPagedList<TEntity, ReturnTentity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        int totalCount = 0;

        var items = query.ToOffsetPage(pageIndex, pageSize, ref totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedList<ReturnTentity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items.Adapt<List<ReturnTentity>>(),
            TotalCount = (int)totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页扩展
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<TEntity> ToPagedList<TEntity>(this IList<TEntity> list, int totalCount, int pageIndex = 1, int pageSize = 20)
       where TEntity : new()
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = list,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }

    /// <summary>
    /// 分页扩展
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">索引</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<TEntity> ToPagedList<TEntity>(this IList<TEntity> list, SqlSugar.PageModel page)
       where TEntity : new()
    {
        return ToPagedList<TEntity>(list, page.TotalCount, page.PageIndex, page.PageSize);
    }

    /// <summary>
    ///分页扩展
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static PagedList<TEntity> ToPagedList<TEntity>(this IQueryable<TEntity> list, int pageIndex = 1, int pageSize = 20)
    {
        var totalPages = (int)Math.Ceiling(list.Count() / (double)pageSize);

        return new PagedList<TEntity>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = list.Count(),
            TotalPages = totalPages,
            HasNextPages = pageIndex < totalPages,
            HasPrevPages = pageIndex - 1 > 0
        };
    }
}