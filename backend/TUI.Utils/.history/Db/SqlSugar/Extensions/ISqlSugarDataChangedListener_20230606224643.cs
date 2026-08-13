// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// SqlSugar 数据改变监听
/// </summary>
public interface ISqlSugarDataChangedListener
{
    /// <summary>
    /// 监听数据改变之后（仅支持EFCore操作）
    /// </summary>
    /// <param name="db"></param>
    /// <param name="dataFilterModel">数据过滤实体</param>
    void OnChanged(ISqlSugarClient db, DataFilterModel dataFilterModel);
}