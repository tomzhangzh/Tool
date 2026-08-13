// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace SqlSugar;

/// <summary>
/// 不自增
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class SugarNoIdentityAttribute : Attribute
{
}