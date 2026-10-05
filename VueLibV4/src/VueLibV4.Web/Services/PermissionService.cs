using System.Security.Cryptography;
using System.Text;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Services;

/// <summary>
/// 权限服务：判断当前用户是否拥有某资源/操作的权限。
/// 前端只做 UI 控制，后端这里是安全校验入口。
/// 业务项目可通过 IPermissionHook 扩展自己的权限逻辑。
/// </summary>
public interface IPermissionService
{
    /// <summary>当前登录用户名；未登录固定为 <see cref="AnonymousUser"/>，绝不兜底为 admin。</summary>
    string CurrentUserName { get; }

    /// <summary>是否已通过登录（签名 cookie）/Windows 认证/API Key 服务账户认证。</summary>
    bool IsAuthenticated { get; }

    int CurrentProjectId { get; }

    Task<bool> CanReadAsync(string resourceKey, int projectId = 0);
    Task<bool> CanEditAsync(string resourceKey, int projectId = 0);
    Task<bool> CanDeleteAsync(string resourceKey, int projectId = 0);
    Task<bool> HasOperationAsync(string operationCode, int projectId = 0);

    /// <summary>按表名校验数据操作权限（动态数据接口用）。
    /// requiredLevel: Read / Edit / Delete。
    /// 表未绑定任何资源节点 = 放行（未配置权限不阻塞）。</summary>
    Task<bool> CheckTableAsync(string tableName, string requiredLevel);

    Task<HashSet<string>> GetAllKeysAsync();
}

/// <summary>未登录访问的统一匿名身份（不享有任何角色，且不是 admin）。</summary>
public static class PermissionIdentity
{
    public const string AnonymousUser = "anonymous";
    public const string UserCookieName = "dyn_user";

    /// <summary>ApiKeyAuthMiddleware 校验通过后在 HttpContext.Items 打的标记，过滤器据此放行机器调用。</summary>
    public const string ApiAuthenticatedItem = "Dyn.ApiAuthenticated";
}

/// <summary>
/// 登录 cookie 签名票据：cookie = urlencode(userName) + '.' + HMACSHA256(userName, secret)。
/// 明文部分仅用于回读用户名，防篡改靠 HMAC；secret 取配置 Dyn:Auth:CookieSecret。
/// 未配置 secret 时退回编译期开发默认值并由 PermissionService 打告警，对外部署必须显式配置。
/// </summary>
public static class AuthTicket
{
    public const string DevFallbackSecret = "vue-lib-v4-dev-only-cookie-secret-CHANGE-ME";

    public static string Issue(string userName, string secret)
    {
        var name = (userName ?? string.Empty).Trim();
        return Uri.EscapeDataString(name) + "." + Sign(name, secret);
    }

    /// <summary>校验票据签名并还原用户名；失败返回 false。</summary>
    public static bool TryValidate(string? ticket, string secret, out string userName)
    {
        userName = string.Empty;
        if (string.IsNullOrEmpty(ticket)) return false;
        var dot = ticket.LastIndexOf('.');
        if (dot <= 0 || dot >= ticket.Length - 1) return false;

        var namePart = ticket[..dot];
        var sig = ticket[(dot + 1)..];
        string name;
        try { name = Uri.UnescapeDataString(namePart); }
        catch { return false; }
        if (string.IsNullOrEmpty(name)) return false;

        var expected = Sign(name, secret);
        return FixedTimeEquals(sig, expected) && TrySet(name, out userName);
    }

    private static bool TrySet(string name, out string userName)
    {
        userName = name;
        return true;
    }

    private static string Sign(string userName, string secret)
    {
        var key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(secret) ? DevFallbackSecret : secret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userName));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
}

public class PermissionService : IPermissionService
{
    private readonly ISysResourcePermissionService _permSvc;
    private readonly ISysResourceService _resSvc;
    private readonly IHttpContextAccessor _http;
    private readonly IEnumerable<IPermissionHook> _hooks;
    private readonly IConfiguration _config;
    private readonly ILogger<PermissionService> _logger;
    private HashSet<string>? _cache;
    private string? _resolvedUser;
    private bool _userResolved;
    private bool? _authenticated;

    public PermissionService(
        ISysResourcePermissionService permSvc,
        ISysResourceService resSvc,
        IHttpContextAccessor http,
        IEnumerable<IPermissionHook> hooks,
        IConfiguration config,
        ILogger<PermissionService> logger)
    {
        _permSvc = permSvc;
        _resSvc = resSvc;
        _http = http;
        _hooks = hooks;
        _config = config;
        _logger = logger;
    }

    private string CookieSecret
    {
        get
        {
            var secret = _config["Dyn:Auth:CookieSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("[Auth] 未配置 Dyn:Auth:CookieSecret，登录票据使用内置开发默认密钥；对外部署前必须在 appsettings 中配置独立密钥");
                return AuthTicket.DevFallbackSecret;
            }
            return secret;
        }
    }

    public string CurrentUserName
    {
        get
        {
            EnsureIdentityResolved();
            return _resolvedUser ?? PermissionIdentity.AnonymousUser;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            EnsureIdentityResolved();
            return _authenticated ?? false;
        }
    }

    private void EnsureIdentityResolved()
    {
        if (_userResolved) return;
        _userResolved = true;

        var ctx = _http.HttpContext;
        // 1) 签名登录 cookie：验签通过才采信，浏览器手工改 cookie 无法冒充
        if (ctx?.Request?.Cookies?.TryGetValue(PermissionIdentity.UserCookieName, out var ticket) == true
            && AuthTicket.TryValidate(ticket, CookieSecret, out var name))
        {
            _resolvedUser = name;
            _authenticated = true;
            return;
        }
        // 2) API Key 中间件认证通过的机器调用：服务账户，不关联具体用户
        if (ctx is not null && ctx.Items.TryGetValue(PermissionIdentity.ApiAuthenticatedItem, out var api) && api is bool b && b)
        {
            _resolvedUser = "apikey";
            _authenticated = true;
            return;
        }
        // 3) Windows 认证（如后续接入）
        var win = ctx?.User?.Identity;
        if (win?.IsAuthenticated == true && !string.IsNullOrEmpty(win.Name))
        {
            _resolvedUser = win.Name;
            _authenticated = true;
            return;
        }
        // 4) 未登录：匿名身份，不享受任何权限（绝不再兜底 admin）
        _resolvedUser = PermissionIdentity.AnonymousUser;
        _authenticated = false;
    }

    public int CurrentProjectId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx == null) return 0;
            if (ctx.Request.Query.TryGetValue("projectId", out var pid) && int.TryParse(pid, out var id))
                return id;
            if (ctx.Request.Headers.TryGetValue("X-Project-Id", out var hpid) && int.TryParse(hpid, out var hid))
                return hid;
            return 0;
        }
    }

    public async Task<HashSet<string>> GetAllKeysAsync()
    {
        if (_cache != null) return _cache;
        _cache = await _permSvc.GetPermissionKeysAsync(CurrentUserName);
        foreach (var hook in _hooks)
        {
            try
            {
                var extra = await hook.GetExtraPermissionKeysAsync(CurrentUserName, CurrentProjectId);
                if (extra != null)
                    foreach (var k in extra) _cache.Add(k);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Auth] 权限钩子 {Hook} 执行失败，已忽略", hook.GetType().Name);
            }
        }
        return _cache;
    }

    public async Task<bool> CanReadAsync(string resourceKey, int projectId = 0)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) return true;
        if (IsAdmin) return true;
        if (await AskHookAsync(resourceKey, "Read", projectId) is bool r) return r;
        var keys = await GetAllKeysAsync();
        return keys.Contains($"{resourceKey}|Read") || keys.Contains($"{resourceKey}|Edit");
    }

    public async Task<bool> CanEditAsync(string resourceKey, int projectId = 0)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) return true;
        if (IsAdmin) return true;
        if (await AskHookAsync(resourceKey, "Edit", projectId) is bool r) return r;
        return (await GetAllKeysAsync()).Contains($"{resourceKey}|Edit");
    }

    public async Task<bool> CanDeleteAsync(string resourceKey, int projectId = 0)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) return true;
        if (IsAdmin) return true;
        if (await AskHookAsync(resourceKey, "Delete", projectId) is bool r) return r;
        return (await GetAllKeysAsync()).Contains($"{resourceKey}|Delete");
    }

    public Task<bool> HasOperationAsync(string operationCode, int projectId = 0)
        => CanEditAsync(operationCode, projectId);

    public async Task<bool> CheckTableAsync(string tableName, string requiredLevel)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return true;
        if (IsAdmin) return true;
        // 找绑定该表的资源节点
        var res = _resSvc.FindByTable(tableName);
        if (res == null) return true; // 未绑定资源 = 不限制
        // 根据 requiredLevel 校验
        return requiredLevel switch
        {
            "Read" => await CanReadAsync(res.Key),
            "Edit" => await CanEditAsync(res.Key),
            "Delete" => await CanDeleteAsync(res.Key),
            _ => await CanReadAsync(res.Key)
        };
    }

    private bool IsAdmin
        => IsAuthenticated
           && CurrentUserName.Equals("admin", StringComparison.OrdinalIgnoreCase);

    private async Task<bool?> AskHookAsync(string key, string level, int projectId)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                var r = await hook.HasPermissionAsync(CurrentUserName, projectId, "Resource", key, level);
                if (r.HasValue) return r.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Auth] 权限钩子 {Hook} 执行失败，已忽略", hook.GetType().Name);
            }
        }
        return null;
    }
}
