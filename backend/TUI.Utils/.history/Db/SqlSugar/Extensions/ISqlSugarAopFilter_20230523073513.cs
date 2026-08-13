// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// SqlSugar AOP 过滤器
/// </summary>
public interface ISqlSugarAopFilter
{
    /// <summary>
    /// 全局过滤器
    /// </summary>
    /// <param name="db"></param>
    void Filter(ISqlSugarClient dbProvider);
}