// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 创建接口
/// </summary>
public interface ICreate
{
    /// <summary>
    /// 创建用户ID
    /// </summary>
    long? CreatorUserId { get; set; }

    /// <summary>
    /// 创建用户名
    /// </summary>
    string CreatorUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTimeOffset CreationTime { get; set; }
}