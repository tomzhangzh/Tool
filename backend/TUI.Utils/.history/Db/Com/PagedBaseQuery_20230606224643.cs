// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 分页参数
/// </summary>
public class PagedBaseQuery
{
    /// <summary>
    /// 页数
    /// </summary>
    private int _pageIndex = 1;

    /// <summary>
    /// 页数
    /// </summary>
    public int PageIndex
    {
        get
        {
            if (_pageIndex > 0)
            {
                return _pageIndex;
            }
            return 1;
        }
        set => _pageIndex = value;
    }

    /// <summary>
    /// 每页条数
    /// </summary>
    private int _pageSize = 10;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize
    {
        get
        {
            if (_pageSize > 0)
            {
                return _pageSize;
            }
            return 10;
        }
        set => _pageSize = value;
    }
}