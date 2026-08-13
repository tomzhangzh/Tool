// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// Layui 分页扩展
/// </summary>
public static class LayuiPagedExtensions
{
    /// <summary>
    /// Layui分页扩展(返回支持layui格式的数据)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="totalCount">总数据条数</param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static LayuiPagedList<TEntity> ToLayuiPagedList<TEntity>(this IList<TEntity> list, int totalCount, int pageIndex = 1, int pageSize = 20)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = list,
            count = totalCount,
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// Layui分页扩展(返回支持layui格式的数据)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public static LayuiPagedList<TEntity> ToLayuiPagedList<TEntity>(this IQueryable<TEntity> list, int pageIndex = 1, int pageSize = 20)
    {
        var totalPages = (int)Math.Ceiling(list.Count() / (double)pageSize);

        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            count = list.Count(),
            totalpage = totalPages,
        };
    }

    /// <summary>
    /// 分页扩展(返回支持layui格式的数据)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<LayuiPagedList<TEntity>> ToLayuiPagedListAsync<TEntity>(this IQueryable<TEntity> entities, int pageIndex = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var totalCount = await entities.CountAsync(cancellationToken);
        var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items,
            count = totalCount,
            totalpage = totalPages
        };
    }

    /// <summary>
    /// Layui分页
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="list">数据集合</param>
    /// <param name="page">分页对象</param>
    /// <returns></returns>
    public static LayuiPagedList<TEntity> SqlSugarPagedListToLayui<TEntity>(this IList<TEntity> list, SqlSugar.PageModel page)
    {
        return ToLayuiPagedList<TEntity>(list, page.TotalCount, page.PageIndex, page.PageSize);
    }

    /// <summary>
    /// 分页扩展(返回支持layui格式的数据)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    /// <param name="pageIndex">查询页数</param>
    /// <param name="pageSize">条数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<LayuiPagedList<TEntity>> ToLayuiPagedListAsync<TEntity>(this IList<TEntity> entities, int pageIndex = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var totalCount = await entities.CountAsync(cancellationToken);
        var items = await entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LayuiPagedList<TEntity>
        {
            page = pageIndex,
            limit = pageSize,
            data = items,
            count = totalCount,
            totalpage = totalPages
        };
    }
}