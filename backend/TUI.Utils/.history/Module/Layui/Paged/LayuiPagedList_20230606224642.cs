// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 数据集合(layui格式的数据)
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class LayuiPagedList<TEntity>
{
    public IEnumerable<TEntity> data { get; set; }

    /// <summary>
    /// 总数据条数
    /// </summary>
    public int count { get; set; }

    /// <summary>
    /// 状态码(默认为0)
    /// </summary>

    public int code { get; set; } = 0;

    /// <summary>
    /// 消息
    /// </summary>
    public string msg { get; set; } = "";

    /// <summary>
    /// 当前索引页
    /// </summary>
    public int page { get; set; }

    /// <summary>
    /// 每页显示条数
    /// </summary>
    public int limit { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int totalpage { get; set; }
}