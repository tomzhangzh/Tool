namespace VueLibV4.Platform.Models;

/// <summary>
/// 统一动态数据端点常量（全系统唯一数据入口）：数据域由请求 project 坐标决定
/// （空=默认业务库；"__platform__"=平台元数据库；DynProject.Code/Id=项目库）。
/// Web 层的 <c>DynPageViewHelper</c> 与平台层运行时装配共用同一份常量，避免 URL 双写漂移。
/// </summary>
public static class DynDataEndpoints
{
    public const string Search = "/api/dyndata/search";
    public const string Insert = "/api/dyndata/insert";
    public const string Update = "/api/dyndata/update";
    public const string Delete = "/api/dyndata/delete";
    public const string Get = "/api/dyndata/get";

    /// <summary>按主键自动判别增/改的保存端点（Block 写操作标准入口）。</summary>
    public const string Save = "/api/dyndata/save";

    /// <summary>平台元数据库的数据域坐标（与 ProjectDbResolver.PlatformProjectKey 对应）。</summary>
    public const string PlatformProject = "__platform__";
}
