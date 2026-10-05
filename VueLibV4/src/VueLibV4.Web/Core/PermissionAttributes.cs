namespace VueLibV4.Web.Core;

/// <summary>
/// 资源权限标记：打在 Controller/Action 上。
/// level: Read / Edit / Delete，默认 Read。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ResourcePermissionAttribute : Attribute
{
    public string ResourceKey { get; }
    public string Level { get; }

    public ResourcePermissionAttribute(string resourceKey, string level = "Read")
    {
        ResourceKey = resourceKey;
        Level = level;
    }
}

/// <summary>
/// 操作权限标记：等价 CanEdit(resourceKey)。
/// 例如 [OperationPermission("Customer:Export")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class OperationPermissionAttribute : Attribute
{
    public string ResourceKey { get; }

    public OperationPermissionAttribute(string resourceKey)
    {
        ResourceKey = resourceKey;
    }
}

/// <summary>跳过权限校验</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AllowAnonymousPermissionAttribute : Attribute { }
