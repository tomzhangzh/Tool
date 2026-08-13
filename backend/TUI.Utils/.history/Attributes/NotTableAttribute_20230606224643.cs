// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 非实体表特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class)]
public class NotTableAttribute : Attribute
{
}