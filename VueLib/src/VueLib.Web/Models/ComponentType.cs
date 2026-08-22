namespace VueLib.Web.Models;

/// <summary>
/// 组件类型枚举
/// </summary>
public enum ComponentType
{
    /// <summary>公共组件 - 全局注册，可在任意页面使用</summary>
    Common = 1,

    /// <summary>页面组件 - 对应 Vue Router 路由</summary>
    Page = 2
}
