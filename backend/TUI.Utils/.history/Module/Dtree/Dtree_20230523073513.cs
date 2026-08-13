// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// Dtree 树型菜单
/// </summary>
public class Dtree
{
    public DtreeStatus status { get; set; } = new DtreeStatus()

    {
        code = 200,
        message = "获取成功"
    };

    public dynamic data { get; set; }
}

/// <summary>
/// Dtree 状态
/// </summary>
public class DtreeStatus
{
    public int code { get; set; }
    public string message { get; set; }
}

/// <summary>
/// Dtree 节点
/// </summary>
public class DtreeItem
{
    public string id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public string parentId { get; set; }

    public List<DtreeItem> children { get; set; } = new List<DtreeItem>();

    /// <summary>
    /// //是否最后一级节点（true：是，false：否，布尔值，非必填）
    /// </summary>
    public bool last { get; set; }
}

/// <summary>
/// Dtree选中的节点
/// </summary>
public class DtreeCheckItem
{
    public string id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public string parentId { get; set; }

    public List<DtreeCheckItem> children { get; set; } = new List<DtreeCheckItem>();

    /// <summary>
    /// //是否最后一级节点（true：是，false：否，布尔值，非必填）
    /// </summary>
    public bool last { get; set; }

    /// <summary>
    ///  checked: "checked", //是否选中（开启复选框，0-未选中，1-选中，2-半选。非必填）
    /// type: "type", //复选框标记（开启复选框，从0开始。非必填）
    /// </summary>
    public Dictionary<string, string> checkArr { get; set; } = new Dictionary<string, string>();
}

public class DtreeEntity
{
    public string id { get; set; }

    public string name { get; set; }

    public string pid { get; set; }

    /// <summary>
    /// 排序，默认为0
    /// </summary>
    public int sort { get; set; } = 0;
}