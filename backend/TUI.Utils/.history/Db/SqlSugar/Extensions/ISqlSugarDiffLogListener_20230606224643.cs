// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// SqlSugar 差异日志
/// </summary>
public interface ISqlSugarDiffLogListener
{
    /// <summary>
    /// 差异日志
    /// </summary>
    /// <param name="db"></param>
    /// <param name="diffLogModel">差异日志</param>
    void DiffLog(ISqlSugarClient db, DiffLogModel diffLogModel);
}