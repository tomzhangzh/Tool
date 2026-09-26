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
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;

    public ProjectDbResolver(DbFactory dbs, DynamicCrudService svc)
    {
        _dbs = dbs;
        _svc = svc;
    }

    /// <summary>按 DynProject.Code 或数字 Id 解析业务库；缺省/失败回退 BusinessDb</summary>
    public SqlSugarClient Resolve(string project)
    {
        if (string.IsNullOrWhiteSpace(project))
            return _dbs.BusinessDb();

        using var pdb = _dbs.PlatformDb();
        JObject proj = long.TryParse(project, out var pid)
            ? _svc.First(pdb, "DynProject", "[Id]=@id", new { id = pid })
            : _svc.First(pdb, "DynProject", "[Code]=@code", new { code = project });

        if (proj == null) return _dbs.BusinessDb();
        var cs = proj["ConnectionString"]?.ToString();
        return string.IsNullOrWhiteSpace(cs) ? _dbs.BusinessDb() : _dbs.ProjectDb(cs);
    }
}
