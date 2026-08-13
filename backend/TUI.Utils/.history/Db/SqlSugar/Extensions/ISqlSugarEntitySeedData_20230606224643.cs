// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 实体种子数据接口
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ISqlSugarEntitySeedData<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntity> HasData();
}