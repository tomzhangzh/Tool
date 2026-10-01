using Newtonsoft.Json.Linq;
using SqlSugar;

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

    public ProjectDbResolver(DbFactory dbs, DynamicCrudService svc)
    {
        _dbs = dbs;
        _svc = svc;
    }

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

        using var pdb = _dbs.PlatformDb();
        JObject proj = long.TryParse(project, out var pid)
            ? _svc.First(pdb, "DynProject", "[Id]=@id", new { id = pid })
            : _svc.First(pdb, "DynProject", "[Code]=@code", new { code = project });

        if (proj == null) return _dbs.BusinessDb();
        var cs = proj["ConnectionString"]?.ToString();
        return string.IsNullOrWhiteSpace(cs) ? _dbs.BusinessDb() : _dbs.ProjectDb(cs);
    }
}
