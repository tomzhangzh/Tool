// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// SqlSugar Layui  分页扩展
/// </summary>
public static class SqlSugarLayuiPagedExtensions
{
    /// <summary>
    /// Layui分页拓展
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static async Task<LayuiPagedList<TEntity>> ToLayuiPagedListAsync<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        RefAsync<int> totalCount = 0;
        var items = await query.ToOffsetPageAsync(pageIndex, pageSize, totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items,
            count = (int)totalCount,
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// layui分页拓展
    /// </summary>
    /// <param name="query">ISugarQueryable</param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static LayuiPagedList<TEntity> ToLayuiPagedList<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        int totalCount = 0;

        var items = query.ToOffsetPage(pageIndex, pageSize, ref totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items,
            count = (int)totalCount,
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// Layui分页拓展
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static async Task<LayuiPagedList<ReturnTentity>> ToLayuiPagedListAsync<TEntity, ReturnTentity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        RefAsync<int> totalCount = 0;
        var items = await query.ToOffsetPageAsync(pageIndex, pageSize, totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new LayuiPagedList<ReturnTentity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items.Adapt<List<ReturnTentity>>(),
            count = (int)totalCount,
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// layui分页拓展
    /// </summary>
    /// <param name="query">ISugarQueryable</param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static LayuiPagedList<ReturnTentity> ToLayuiPagedList<TEntity, ReturnTentity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        int totalCount = 0;

        var items = query.ToOffsetPage(pageIndex, pageSize, ref totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new LayuiPagedList<ReturnTentity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items.Adapt<List<ReturnTentity>>(),
            count = (int)totalCount,
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// 分页扩展(返回支持layui格式的数据)
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="pageList"></param>
    /// <returns></returns>
    public static LayuiPagedList<T2> ToLayuiPagedList<T1, T2>(this PagedList<T1> pageList)
    {
        //var totalCount = await entities.CountAsync(cancellationToken);
        //var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        //var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LayuiPagedList<T2>
        {
            page = pageList.PageIndex,
            limit = pageList.PageSize,
            data = pageList.Items.Adapt<List<T2>>(),
            count = pageList.TotalCount,
            totalpage = pageList.PageSize
        };
    }
}