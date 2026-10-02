using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Collections.Concurrent;

namespace VueLibV4.Services.Data;

/// <summary>
/// DynProject.Code / Id → 业务库连接解析（DynDataController 与 DynCommonController 共用）。
/// project 为空、项目不存在或项目未配连接串时，统一回退默认 BusinessDb。
/// 说明：本类以字符串表名免模型读取 DynProject，因此不依赖 VueLibV4.Platform 强类型实体。
/// </summary>
public class ProjectDbResolver
{
    /// <summary>
    /// 保留数据域坐标：平台元数据库（PlatformDb）。
    /// 统一数据端点 /api/dyndata 按 body.project 选择数据域时，本值表示“平台库”——
    /// 它与 DynProject.Code/Id 地位对等，只是不由 DynProject 表定义、不可被项目占用。
    /// </summary>
    public const string PlatformProjectKey = "__platform__";

    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;

    // project → 物理库连接串 缓存。项目连接串极少变，避免每个数据请求都开一次平台库查 DynProject。
    // value 为空串表示"该项目无连接串，回退 BusinessDb"。项目编辑/删除后调 ClearCache。
    private static readonly ConcurrentDictionary<string, string> _connCache = new(StringComparer.OrdinalIgnoreCase);

    public ProjectDbResolver(DbFactory dbs, DynamicCrudService svc)
    {
        _dbs = dbs;
        _svc = svc;
    }

    /// <summary>清空项目连接串缓存（DynProject 增/改/删后调用）。</summary>
    public static void ClearCache() => _connCache.Clear();

    /// <summary>
    /// 按数据域坐标解析物理库：
    ///   "__platform__" → 平台元数据库 PlatformDb；
    ///   空/未匹配 DynProject/项目无连接串 → 默认业务库 BusinessDb；
    ///   DynProject.Code 或数字 Id（且配了连接串）→ 该项目独立库。
    /// </summary>
    public SqlSugarClient Resolve(string project)
    {
        if (string.IsNullOrWhiteSpace(project))
            return _dbs.BusinessDb();

        if (project == PlatformProjectKey)
            return _dbs.PlatformDb();

        // 命中缓存：直接按连接串建库；空串表示回退 BusinessDb。
        if (_connCache.TryGetValue(project, out var cachedCs))
            return string.IsNullOrWhiteSpace(cachedCs) ? _dbs.BusinessDb() : _dbs.ProjectDb(cachedCs);

        using var pdb = _dbs.PlatformDb();
        JObject proj = long.TryParse(project, out var pid)
            ? _svc.First(pdb, "DynProject", "[Id]=@id", new { id = pid })
            : _svc.First(pdb, "DynProject", "[Code]=@code", new { code = project });

        if (proj == null)
        {
            _connCache[project] = "";   // 记成"无项目→回退业务库"，避免反复查不存在的项目
            return _dbs.BusinessDb();
        }
        var cs = proj["ConnectionString"]?.ToString() ?? "";
        _connCache[project] = cs;
        return string.IsNullOrWhiteSpace(cs) ? _dbs.BusinessDb() : _dbs.ProjectDb(cs);
    }
}
