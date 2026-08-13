// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// Layui 分页参数
/// </summary>
public class LayuiPageBaseQuery
{
    /// <summary>
    /// 页数
    /// </summary>
    private int _page = 1;

    /// <summary>
    /// 页数
    /// </summary>
    public int Page
    {
        get
        {
            if (_page > 0)
            {
                return _page;
            }
            return 1;
        }
        set => _page = value;
    }

    /// <summary>
    /// 查询条数
    /// </summary>
    private int _limit = 10;

    /// <summary>
    /// 查询条数
    /// </summary>
    public int Limit
    {
        get
        {
            if (_limit > 0)
            {
                return _limit;
            }
            return 10;
        }
        set => _limit = value;
    }
}