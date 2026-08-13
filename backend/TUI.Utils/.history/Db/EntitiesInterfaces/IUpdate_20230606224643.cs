// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 更新接口
/// </summary>
public interface IUpdate
{
    /// <summary>
    /// 更新用户ID
    /// </summary>
    long? UpdateUserId { get; set; }

    /// <summary>
    /// 更新用户名称
    /// </summary>
    string UpdateUserName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    DateTimeOffset? UpdateTime { get; set; }
}