// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// PearAdmin 菜单
/// </summary>
public class PearMenuItemOut
{
    public object id { get; set; }

    /// <summary>
    /// 界面中所显示的菜单标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 菜单类型 0: 目录 1: 菜单
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 菜单类型下访问的页面
    /// </summary>
    public string href { get; set; }

    /// <summary>
    /// 当 type 为 1 时，openType 生效，_iframe 正常打开 _blank 新建浏览器标签页
    /// </summary>
    public string openType { get; set; }

    /// <summary>
    /// 目录类型下，该目录下菜单的数组数据
    /// </summary>
    public List<PearMenuItemOut> children { get; set; }
}