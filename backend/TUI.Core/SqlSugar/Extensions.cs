using NPOI.SS.Formula.Functions;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core;

public static class PagedQueryableExtensions
{
    /// <summary>
    /// 分页拓展
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static SqlSugarPagedList<TEntity> ToPagedListAsync<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        int totalCount = 0;
        var items = query.ToPageList(pageIndex, pageSize,ref   totalCount);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new SqlSugarPagedList<TEntity>
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
}