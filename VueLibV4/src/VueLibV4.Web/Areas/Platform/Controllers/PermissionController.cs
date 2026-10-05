using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;
using VueLibV4.Web.Services;
using System.Security.Cryptography;
using System.Text;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 权限接口：当前用户权限清单 + 登录登出改密 + 角色/资源树/授权管理。
/// 匿名端点仅 login；me/logout/changepassword 需登录；其余管理端点仅 admin。
/// </summary>
[Area("Platform")]
[Route("api/platform/permission")]
[ApiController]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _perm;
    private readonly ISysRoleService _roles;
    private readonly ISysResourceService _resources;
    private readonly ISysResourcePermissionService _perms;
    private readonly ISysUserRoleService _userRoles;
    private readonly ISysUserService _users;
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _config;

    public PermissionController(
        IPermissionService perm,
        ISysRoleService roles,
        ISysResourceService resources,
        ISysResourcePermissionService perms,
        ISysUserRoleService userRoles,
        ISysUserService users,
        ISqlSugarClient db,
        IConfiguration config)
    {
        _perm = perm;
        _roles = roles;
        _resources = resources;
        _perms = perms;
        _userRoles = userRoles;
        _users = users;
        _db = db;
        _config = config;
    }

    /// <summary>非管理员一律拒绝（角色/资源/用户/授权等管理端点统一守卫）</summary>
    private ApiResult? EnsureAdmin()
        => _perm.CurrentUserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? null
            : ApiResult.Fail("需要管理员权限", 403);

    /// <summary>当前用户权限清单（前端缓存到 sessionStorage）</summary>
    [HttpGet("me")]
    public async Task<ApiResult> Me()
    {
        var keys = await _perm.GetAllKeysAsync();
        return ApiResult.Ok(new
        {
            userName = _perm.CurrentUserName,
            isAdmin = _perm.CurrentUserName.Equals("admin", StringComparison.OrdinalIgnoreCase),
            keys = keys.ToList()
        });
    }

    // ---------------- 登录 ----------------

    /// <summary>登录：验证用户名密码，写签名 cookie。唯一的匿名管理接口。</summary>
    [HttpPost("login")]
    [AllowAnonymousPermission]
    public ApiResult Login([FromBody] JObject body)
    {
        var userName = body?["userName"]?.ToString();
        var password = body?["password"]?.ToString();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return ApiResult.Fail("用户名和密码不能为空");

        var user = _users.List(u => u.UserName == userName && u.IsActive).FirstOrDefault();
        if (user == null) return ApiResult.Fail("用户不存在或已禁用");

        // 兼容历史明文密码：明文或 SHA256 任一匹配即可
        var inputHash = Sha256(password);
        if (user.Password != password && user.Password != inputHash) return ApiResult.Fail("密码错误");

        // 历史明文密码登录成功后自动升级为哈希存储
        if (user.Password == password)
        {
            user.Password = inputHash;
            _users.Update(user);
        }

        // 写签名 cookie（24小时，HttpOnly 防 JS 读取，Lax 防 CSRF，HTTPS 下自动带 Secure）
        var secret = _config["Dyn:Auth:CookieSecret"];
        var ticket = AuthTicket.Issue(user.UserName, string.IsNullOrWhiteSpace(secret) ? AuthTicket.DevFallbackSecret : secret);
        Response.Cookies.Append(PermissionIdentity.UserCookieName, ticket, new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        });

        return ApiResult.Ok(new { userName = user.UserName, displayName = user.DisplayName }, "登录成功");
    }

    /// <summary>登出</summary>
    [HttpPost("logout")]
    public ApiResult Logout()
    {
        Response.Cookies.Delete(PermissionIdentity.UserCookieName);
        return ApiResult.Ok(true, "已退出");
    }

    /// <summary>修改密码（只能改自己的）</summary>
    [HttpPost("changepassword")]
    public ApiResult ChangePassword([FromBody] JObject body)
    {
        var userName = _perm.CurrentUserName;
        var oldPwd = body?["oldPassword"]?.ToString();
        var newPwd = body?["newPassword"]?.ToString();
        if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd))
            return ApiResult.Fail("旧密码和新密码不能为空");

        var user = _users.List(u => u.UserName == userName).FirstOrDefault();
        if (user == null) return ApiResult.Fail("用户不存在");

        // 验证旧密码（明文或哈希）
        var oldHash = Sha256(oldPwd);
        if (user.Password != oldPwd && user.Password != oldHash) return ApiResult.Fail("旧密码错误");

        // 存新密码的哈希
        user.Password = Sha256(newPwd);
        _users.Update(user);
        return ApiResult.Ok(true, "密码修改成功");
    }

    // ---------------- 角色 ----------------

    [HttpGet("roles")]
    public ApiResult Roles()
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        return ApiResult.Ok(_roles.List(r => true, "Id ASC"));
    }

    [HttpPost("role/save")]
    public ApiResult SaveRole([FromBody] SysRole role)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (role == null || string.IsNullOrWhiteSpace(role.Name)) return ApiResult.Fail("角色名不能为空");
        if (role.Id <= 0) _roles.Insert(role); else _roles.Update(role);
        return ApiResult.Ok(new { role.Id }, "保存成功");
    }

    [HttpPost("role/delete")]
    public ApiResult DeleteRole([FromBody] JObject body)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var id = body?["id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 id");
        // 角色授权 + 用户角色映射 + 角色本体必须同删，任一步失败整体回滚，避免孤儿授权
        try
        {
            _db.Ado.BeginTran();
            _perms.Delete(p => p.RoleId == id);
            _userRoles.Delete(u => u.RoleId == id);
            _roles.DeleteById(id);
            _db.Ado.CommitTran();
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            return ApiResult.Fail("删除失败已回滚：" + ex.GetBaseException().Message);
        }
        return ApiResult.Ok(true, "已删除");
    }

    // ---------------- 资源树 ----------------

    /// <summary>全部资源节点（扁平列表，前端自己拼树）</summary>
    [HttpGet("resources")]
    public ApiResult Resources()
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        return ApiResult.Ok(_resources.ListAll());
    }

    [HttpPost("resource/save")]
    public ApiResult SaveResource([FromBody] SysResource res)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (res == null || string.IsNullOrWhiteSpace(res.Key)) return ApiResult.Fail("Key 不能为空");
        if (res.Id <= 0) _resources.Insert(res); else _resources.Update(res);
        return ApiResult.Ok(new { res.Id }, "保存成功");
    }

    [HttpPost("resource/delete")]
    public ApiResult DeleteResource([FromBody] JObject body)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var id = body?["id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 id");
        try
        {
            _db.Ado.BeginTran();
            _perms.Delete(p => p.ResourceId == id);
            _resources.DeleteById(id);
            _db.Ado.CommitTran();
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            return ApiResult.Fail("删除失败已回滚：" + ex.GetBaseException().Message);
        }
        return ApiResult.Ok(true, "已删除");
    }

    // ---------------- 角色授权 ----------------

    /// <summary>取某角色已授权的资源（ResourceId → {Read,Edit,Delete}）</summary>
    [HttpGet("granted")]
    public ApiResult Granted([FromQuery] int roleId)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (roleId <= 0) return ApiResult.Fail("缺少 roleId");
        var list = _perms.List(p => p.RoleId == roleId);
        var dict = list.ToDictionary(x => x.ResourceId, x => new { x.Read, x.Edit, x.Delete });
        return ApiResult.Ok(dict);
    }

    /// <summary>保存角色对某资源的授权</summary>
    [HttpPost("grant")]
    public ApiResult Grant([FromBody] SysResourcePermission data)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (data == null || data.RoleId <= 0 || data.ResourceId <= 0) return ApiResult.Fail("参数错误");
        var existing = _perms.List(p => p.RoleId == data.RoleId && p.ResourceId == data.ResourceId).FirstOrDefault();
        if (existing == null)
        {
            _perms.Insert(data);
        }
        else
        {
            existing.Read = data.Read;
            existing.Edit = data.Edit;
            existing.Delete = data.Delete;
            _perms.Update(existing);
        }
        return ApiResult.Ok(true, "已授权");
    }

    /// <summary>从代码扫描 [ResourcePermission]/[OperationPermission] 特性，自动注册新资源节点（归到"未分配"父节点下）</summary>
    [HttpPost("resource/scan")]
    public ApiResult ScanResources()
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var added = 0;
        var allResources = _resources.ListAll();
        var existingKeys = allResources.Select(r => r.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 确保有一个"未分配"根节点
        var unassigned = allResources.FirstOrDefault(r => r.Key == "__unassigned__");
        if (unassigned == null)
        {
            unassigned = new SysResource { Key = "__unassigned__", Name = "未分配", Type = "Module", ParentId = 0, IsActive = true };
            _resources.Insert(unassigned);
            allResources.Add(unassigned);
        }

        // 扫描所有已加载程序集里的 Controller 和 Action
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft"));

        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); } catch { continue; }
            foreach (var t in types)
            {
                // 类级别的 [ResourcePermission]
                var classAttrs = t.GetCustomAttributes(typeof(ResourcePermissionAttribute), true);
                foreach (ResourcePermissionAttribute attr in classAttrs)
                {
                    if (!existingKeys.Contains(attr.ResourceKey))
                    {
                        _resources.Insert(new SysResource
                        {
                            Key = attr.ResourceKey,
                            Name = attr.ResourceKey,
                            Type = "Page",
                            ParentId = unassigned.Id,
                            HasRead = true,
                            HasEdit = true,
                            HasDelete = false,
                            IsActive = true
                        });
                        existingKeys.Add(attr.ResourceKey);
                        added++;
                    }
                }
                // 方法级别的 [OperationPermission] / [ResourcePermission]
                System.Reflection.MethodInfo[] methods;
                try { methods = t.GetMethods(); } catch { continue; }
                foreach (var m in methods)
                {
                    var attrs = m.GetCustomAttributes(typeof(ResourcePermissionAttribute), true);
                    foreach (ResourcePermissionAttribute attr in attrs)
                    {
                        if (!existingKeys.Contains(attr.ResourceKey))
                        {
                            _resources.Insert(new SysResource
                            {
                                Key = attr.ResourceKey,
                                Name = attr.ResourceKey,
                                Type = "Operation",
                                ParentId = unassigned.Id,
                                HasRead = true,
                                HasEdit = true,
                                HasDelete = false,
                                IsActive = true
                            });
                            existingKeys.Add(attr.ResourceKey);
                            added++;
                        }
                    }
                }
            }
        }
        return ApiResult.Ok(new { added, total = _resources.ListAll().Count }, $"扫描完成，新增 {added} 个资源到「未分配」");
    }

    // ---------------- 用户管理 ----------------

    /// <summary>用户列表（密码字段不下发前端）</summary>
    [HttpGet("users")]
    public ApiResult Users()
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var list = _users.ListAll()
            .Select(u => new { u.Id, u.UserName, u.DisplayName, u.Email, u.IsActive, u.CreateTime });
        return ApiResult.Ok(list);
    }

    /// <summary>新建/编辑用户。
    /// 新建：密码必填，服务端 SHA256 入库；编辑：密码留空=不修改，非空=改密（同样哈希）。
    /// admin 账户不可改名、不可停用，避免锁死系统。</summary>
    [HttpPost("user/save")]
    public ApiResult SaveUser([FromBody] SysUser user)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (user == null || string.IsNullOrWhiteSpace(user.UserName)) return ApiResult.Fail("用户名不能为空");

        if (user.Id <= 0)
        {
            if (string.IsNullOrWhiteSpace(user.Password)) return ApiResult.Fail("新建用户必须设置初始密码");
            if (_users.List(u => u.UserName == user.UserName).Any()) return ApiResult.Fail("用户名已存在");
            user.Password = Sha256(user.Password);
            _users.Insert(user);
            return ApiResult.Ok(new { user.Id }, "保存成功");
        }

        var existing = _users.GetById(user.Id);
        if (existing == null) return ApiResult.Fail("用户不存在");

        var isAdminAccount = existing.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        if (isAdminAccount)
        {
            // 硬编码超管：禁止改名/停用，防止把自己锁在门外
            user.UserName = existing.UserName;
            user.IsActive = true;
        }
        else if (!string.Equals(existing.UserName, user.UserName, StringComparison.Ordinal))
        {
            // 改名：SysUserRole 按 UserName 关联，必须同步迁移，否则角色静默丢失
            if (_users.List(u => u.UserName == user.UserName).Any()) return ApiResult.Fail("用户名已存在");
            try
            {
                var oldName = existing.UserName;
                _db.Ado.BeginTran();
                ApplyUserUpdate(existing, user);
                _db.Updateable<SysUserRole>()
                    .SetColumns(r => r.UserName == user.UserName)
                    .Where(r => r.UserName == oldName)
                    .ExecuteCommand();
                _db.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                _db.Ado.RollbackTran();
                return ApiResult.Fail("保存失败已回滚：" + ex.GetBaseException().Message);
            }
            return ApiResult.Ok(new { user.Id }, "保存成功");
        }

        ApplyUserUpdate(existing, user);
        return ApiResult.Ok(new { user.Id }, "保存成功");
    }

    /// <summary>把表单值落到受管字段；密码仅在非空时按哈希覆盖（空=保留原密码）。</summary>
    private void ApplyUserUpdate(SysUser existing, SysUser input)
    {
        existing.UserName = input.UserName;
        existing.DisplayName = input.DisplayName;
        existing.Email = input.Email;
        existing.IsActive = input.IsActive;
        if (!string.IsNullOrWhiteSpace(input.Password))
            existing.Password = Sha256(input.Password);
        _users.Update(existing);
        // 回传 Id 供接口信封使用
        input.Id = existing.Id;
    }

    /// <summary>删除用户（admin 账户禁止删除；用户-角色映射同事务清理）</summary>
    [HttpPost("user/delete")]
    public ApiResult DeleteUser([FromBody] JObject body)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var id = body?["id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 id");
        var u = _users.GetById(id);
        if (u == null) return ApiResult.Fail("用户不存在");
        if (u.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("内置管理员账户不允许删除");
        try
        {
            _db.Ado.BeginTran();
            _userRoles.Delete(ur => ur.UserName == u.UserName);
            _users.DeleteById(id);
            _db.Ado.CommitTran();
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            return ApiResult.Fail("删除失败已回滚：" + ex.GetBaseException().Message);
        }
        return ApiResult.Ok(true, "已删除");
    }

    // ---------------- 用户-角色 ----------------

    /// <summary>查某角色下所有用户</summary>
    [HttpGet("userroles")]
    public ApiResult UserRoles([FromQuery] int roleId)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        if (roleId <= 0) return ApiResult.Fail("缺少 roleId");
        var list = _userRoles.List(u => u.RoleId == roleId, "Id ASC");
        return ApiResult.Ok(list);
    }

    /// <summary>加用户到角色</summary>
    [HttpPost("userrole/add")]
    public ApiResult AddUserToRole([FromBody] JObject body)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var userName = body?["userName"]?.ToString();
        var roleId = body?["roleId"]?.Value<int>() ?? 0;
        if (string.IsNullOrWhiteSpace(userName) || roleId <= 0) return ApiResult.Fail("缺少 userName 或 roleId");
        var exists = _userRoles.List(u => u.RoleId == roleId && u.UserName == userName).Any();
        if (!exists) _userRoles.Insert(new SysUserRole { UserName = userName, RoleId = roleId });
        return ApiResult.Ok(true, "已添加");
    }

    /// <summary>从角色移除用户</summary>
    [HttpPost("userrole/remove")]
    public ApiResult RemoveUserFromRole([FromBody] JObject body)
    {
        var deny = EnsureAdmin();
        if (deny != null) return deny;
        var id = body?["id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 id");
        _userRoles.DeleteById(id);
        return ApiResult.Ok(true, "已移除");
    }

    /// <summary>SHA256 哈希（密码存储）。无盐快哈希仅适配内网工具定位，对外部署应替换为 PBKDF2/BCrypt。</summary>
    private static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
