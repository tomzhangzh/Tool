namespace VueLibV4.Web.Services;

/// <summary>
/// 行级数据权限示例：业务项目可以参考这个实现。
///
/// 场景：只能看自己创建的数据（CreatedBy = 当前用户）。
/// 实际业务项目可以从数据库/HR/AD 拉真实数据。
/// </summary>
public class DemoRowLevelHook : IPermissionHook
{
    public Task<bool?> HasPermissionAsync(
        string userName,
        int projectId,
        string resourceType,
        string resourceKey,
        string requiredPermission)
    {
        // 示例：对 "订单" 资源，只能看自己创建的
        if (resourceKey == "Order" && requiredPermission == "Read")
        {
            // 实际项目：查数据库当前用户是不是订单的创建人
            // 这里演示：admin 全放行，其他人只看自己的
            // 返回 null = 不干预，走默认 RBAC
            return Task.FromResult<bool?>(null);
        }

        return Task.FromResult<bool?>(null);
    }

    public Task<IEnumerable<string>?> GetExtraPermissionKeysAsync(string userName, int projectId)
    {
        // 示例：从 HR 系统拉当前用户的额外角色
        // 比如：销售总监 → 额外加 "Sales.Manager" 权限
        if (userName == "admin")
        {
            return Task.FromResult<IEnumerable<string>?>(new[] { "Sales.Manager" });
        }
        return Task.FromResult<IEnumerable<string>?>(null);
    }
}
