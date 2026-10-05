namespace VueLibV4.Web.Services;

/// <summary>
/// 业务项目权限钩子：每个业务项目可以自己实现这个接口，
/// 覆盖或补充平台默认的 RBAC 判断。
///
/// 典型用途：
/// 1. 行级数据权限（只看自己部门的数据）
/// 2. 特殊动态判断（周末不能审批、金额超过阈值要二次授权）
/// 3. 从外部系统（AD/HR）拉额外角色
///
/// 注册方式：业务项目在自己的 Startup/Program 里
///   services.AddScoped&lt;IPermissionHook, MyProjectPermissionHook&gt;();
/// 平台 PermissionService 会自动调用所有注册的钩子。
/// </summary>
public interface IPermissionHook
{
    /// <summary>
    /// 判断当前用户对某资源是否有权限。
    /// 返回 true=放行，false=拒绝，null=不干预（走默认 RBAC）。
    /// </summary>
    /// <param name="userName">当前用户名</param>
    /// <param name="projectId">业务项目 Id（0=平台级）</param>
    /// <param name="resourceType">Menu / WebPage / Table / Field / Operation</param>
    /// <param name="resourceKey">资源标识</param>
    /// <param name="requiredPermission">Read / Edit</param>
    Task<bool?> HasPermissionAsync(
        string userName,
        int projectId,
        string resourceType,
        string resourceKey,
        string requiredPermission);

    /// <summary>
    /// 追加当前用户的额外权限 Key（业务项目从外部系统拉的角色/权限）。
    /// 平台默认 RBAC 之外，业务项目可以往清单里塞自己的权限编码。
    /// </summary>
    /// <returns>追加的权限 Key 列表，null/空=无追加</returns>
    Task<IEnumerable<string>?> GetExtraPermissionKeysAsync(string userName, int projectId);
}
