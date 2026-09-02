using System.Text.RegularExpressions;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 动态工程服务：工程 CRUD / 连接测试 / 元数据库表结构发现 / 页面定义生成 / 页面 CRUD / SQL 视图生成
/// </summary>
public class DynProjectService
{
    private readonly AppDbContext _db;
    private readonly DynCrudService _crud;

    public DynProjectService(AppDbContext db, DynCrudService crud)
    {
        _db = db;
        _crud = crud;
    }

    // ==================== 工程 ====================

    public List<DynProject> GetProjects()
    {
        using var db = _db.Create();
        return db.Queryable<DynProject>().OrderBy(p => p.Id).ToList();
    }

    public DynProject? GetProject(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynProject>().InSingle(id);
    }

    public OpResult SaveProject(DynProject p)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) return Op(false, "工程名称不能为空");
        if (string.IsNullOrWhiteSpace(p.ConnectionString)) return Op(false, "数据库连接串不能为空");
        using var db = _db.Create();
        p.Name = p.Name.Trim();
        p.UpdatedAt = DateTime.UtcNow;
        if (p.Id == 0)
        {
            var exists = db.Queryable<DynProject>().Where(x => x.Name == p.Name).Any();
            if (exists) return Op(false, "工程名称已存在");
            p.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(p).ExecuteReturnIdentity();
            return Op(true, "创建成功", new { Id = (int)id });
        }
        db.Updateable(p).ExecuteCommand();
        return Op(true, "保存成功", p);
    }

    public OpResult DeleteProject(int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynPage>().Where(x => x.ProjectId == id).ExecuteCommand();
        db.Deleteable<DynProject>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    /// <summary>测试连接串是否可用</summary>
    public OpResult TestConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return Op(false, "连接串为空");
        try
        {
            using var sc = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });
            sc.Ado.Open();
            sc.Ado.Close();
            return Op(true, "连接成功");
        }
        catch (Exception ex)
        {
            return Op(false, "连接失败：" + ex.Message);
        }
    }

    /// <summary>为工程创建 SqlSugar 客户端（连接工程自己的数据库）</summary>
    public ISqlSugarClient CreateProjectClient(DynProject p)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = p.ConnectionString,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    /// <summary>读取某表列信息（供控制器调用）</summary>
    public List<DbColumnInfo> DynCrudColumns(ISqlSugarClient db, string table)
        => _crud.GetColumns(db, table);

    // ==================== 元数据发现 ====================

    public OpResult GetTables(int projectId)
    {
        var p = GetProject(projectId);
        if (p == null) return Op(false, "工程不存在");
        try
        {
            using var db = CreateProjectClient(p);
            var tables = db.DbMaintenance.GetTableInfoList(false)
                .Where(t => !t.Name.StartsWith("sys") && !t.Name.StartsWith("dt"))
                .OrderBy(t => t.Name)
                .Select(t => new { t.Name, t.Description })
                .ToList();
            return Op(true, "ok", tables);
        }
        catch (Exception ex) { return Op(false, "连接工程数据库失败：" + ex.Message); }
    }

    public OpResult GetColumns(int projectId, string table)
    {
        var p = GetProject(projectId);
        if (p == null) return Op(false, "工程不存在");
        try
        {
            using var db = CreateProjectClient(p);
            var cols = _crud.GetColumns(db, table)
                .OrderBy(c => c.DbColumnName)
                .Select(c => new
                {
                    c.DbColumnName,
                    c.DataType,
                    c.Length,
                    c.IsNullable,
                    c.IsPrimarykey,
                    c.IsIdentity,
                    c.ColumnDescription
                })
                .ToList();
            return Op(true, "ok", cols);
        }
        catch (Exception ex) { return Op(false, "读取列失败：" + ex.Message); }
    }

    // ==================== 页面定义生成 ====================

    /// <summary>
    /// 依据表结构自动生成页面定义（列类型 → 控件/筛选/表格/表单 启发式映射）
    /// </summary>
    public DynPageDefinition GenerateDefinition(string table, List<DbColumnInfo> cols)
    {
        var def = new DynPageDefinition();
        var pk = cols.FirstOrDefault(c => c.IsPrimarykey);
        if (pk != null)
        {
            def.PrimaryKey = pk.DbColumnName;
            def.IsIdentity = pk.IsIdentity;
        }
        else
        {
            def.PrimaryKey = cols.FirstOrDefault()?.DbColumnName ?? "Id";
            def.IsIdentity = false;
        }
        def.OrderBy = def.PrimaryKey;
        def.OrderDir = "desc";

        int order = 0;
        foreach (var c in cols)
        {
            var col = MapColumn(c, order++);
            def.Columns.Add(col);
        }
        return def;
    }

    private DynColumnDef MapColumn(DbColumnInfo c, int order)
    {
        var name = c.DbColumnName;
        var sqlType = (c.DataType ?? "").ToLowerInvariant();
        var dbType = "string";
        if (sqlType.Contains("int") || sqlType.Contains("bigint") || sqlType.Contains("smallint") || sqlType.Contains("tinyint"))
            dbType = sqlType.Contains("bigint") || sqlType.Contains("tinyint") || sqlType.Contains("smallint") ? "long" : "int";
        else if (sqlType.Contains("decimal") || sqlType.Contains("numeric") || sqlType.Contains("money"))
            dbType = "decimal";
        else if (sqlType.Contains("float") || sqlType.Contains("real"))
            dbType = "decimal";
        else if (sqlType.Contains("datetime") || sqlType.Contains("date") || sqlType.Contains("time") || sqlType.Contains("smalldatetime"))
            dbType = "datetime";
        else if (sqlType == "bit")
            dbType = "bool";
        else if (sqlType.Contains("uniqueidentifier"))
            dbType = "guid";

        var col = new DynColumnDef
        {
            Name = name,
            Label = c.ColumnDescription ?? name,
            DbType = dbType,
            SqlType = sqlType,
            IsPrimary = c.IsPrimarykey,
            IsIdentity = c.IsIdentity,
            IsNullable = c.IsNullable,
            Order = order
        };

        // 控件类型
        switch (dbType)
        {
            case "bool": col.Control = "switch"; break;
            case "datetime": col.Control = sqlType.Contains("date") && !sqlType.Contains("datetime") ? "date" : "datetime"; break;
            case "int":
            case "long":
            case "decimal": col.Control = "input-number"; break;
            case "guid": col.Control = "input"; col.IsReadOnly = true; break;
            default: col.Control = "input"; break;
        }

        if (col.IsPrimary)
        {
            col.IsGrid = true;
            col.IsForm = c.IsIdentity;   // 自增主键在表单展示但只读
            col.IsFilter = false;
            col.IsReadOnly = true;
            col.Width = 90;
        }
        else
        {
            col.IsGrid = true;
            col.IsForm = true;
            col.IsFilter = dbType is "string" or "int" or "long" or "decimal" or "datetime";
            col.FilterOp = dbType == "string" ? "like" : "eq";
            col.Required = !c.IsNullable;
            col.Width = dbType switch
            {
                "string" => 140,
                "datetime" => 170,
                _ => 110
            };
            // 常见下拉/开关启发式
            var lower = name.ToLowerInvariant();
            if (lower.Contains("gender") || name.Contains("性别"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("男", "男"), new("女", "女") };
            }
            else if (lower.Contains("level") || lower.Contains("grade") || name.Contains("等级") || name.Contains("级别"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("初级", "初级"), new("中级", "中级"), new("高级", "高级") };
            }
            else if (lower.Contains("status") || lower.Contains("state") || name.Contains("状态") || name.Contains("启用") || name.Contains("是否"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("启用", "启用"), new("停用", "停用") };
            }
            if (col.Control == "select" && col.Options.Count == 0)
                col.Control = "input";
        }

        return col;
    }

    // ==================== 页面 CRUD ====================

    public List<DynPage> GetPages(int projectId)
    {
        using var db = _db.Create();
        return db.Queryable<DynPage>().Where(x => x.ProjectId == projectId).OrderBy(x => x.PageType).OrderBy(x => x.Name).ToList();
    }

    public DynPage? GetPage(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynPage>().InSingle(id);
    }

    public OpResult SavePage(DynPage p)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) return Op(false, "页面编码不能为空");
        if (string.IsNullOrWhiteSpace(p.TableName)) return Op(false, "请选择表");
        if (string.IsNullOrWhiteSpace(p.ColumnDefs)) return Op(false, "页面定义不能为空");
        using var db = _db.Create();
        p.Name = p.Name.Trim();
        p.UpdatedAt = DateTime.UtcNow;
        if (p.Id == 0)
        {
            var exists = db.Queryable<DynPage>().Where(x => x.ProjectId == p.ProjectId && x.Name == p.Name && x.PageType == p.PageType).Any();
            if (exists) return Op(false, "同工程的页面编码已存在");
            p.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(p).ExecuteReturnIdentity();
            return Op(true, "保存成功", new { Id = (int)id });
        }
        db.Updateable(p).ExecuteCommand();
        return Op(true, "保存成功", p);
    }

    public OpResult DeletePage(int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynPage>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    // ==================== 设计层：模板管理 + 路由页面管理 ====================

    public List<DynTemplate> GetTemplates(int projectId)
    {
        using var db = _db.Create();
        return db.Queryable<DynTemplate>().Where(x => x.ProjectId == projectId).OrderBy(x => x.SortOrder).OrderBy(x => x.Name).ToList();
    }

    public DynTemplate? GetTemplate(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynTemplate>().InSingle(id);
    }

    public OpResult SaveTemplate(DynTemplate t)
    {
        if (string.IsNullOrWhiteSpace(t.Name)) return Op(false, "模板名称不能为空");
        if (string.IsNullOrWhiteSpace(t.Code)) return Op(false, "模板编码不能为空");
        using var db = _db.Create();
        t.Code = t.Code.Trim();
        t.UpdatedAt = DateTime.UtcNow;
        if (t.Id == 0)
        {
            if (db.Queryable<DynTemplate>().Any(x => x.ProjectId == t.ProjectId && x.Code == t.Code))
                return Op(false, "同工程的模板编码已存在");
            t.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(t).ExecuteReturnIdentity();
            return Op(true, "保存成功", new { Id = (int)id });
        }
        db.Updateable(t).ExecuteCommand();
        return Op(true, "保存成功", t);
    }

    public OpResult DeleteTemplate(int id)
    {
        using var db = _db.Create();
        // 被路由页面引用时禁止删除
        if (db.Queryable<DynWebPage>().Any(x => x.TemplateId == id))
            return Op(false, "该模板正被路由页面使用，请先解除引用");
        db.Deleteable<DynTemplate>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    public List<DynWebPage> GetWebPages(int projectId)
    {
        using var db = _db.Create();
        return db.Queryable<DynWebPage>().Where(x => x.ProjectId == projectId).OrderByDescending(x => x.IsHome).OrderBy(x => x.SortOrder).OrderBy(x => x.Name).ToList();
    }

    public DynWebPage? GetWebPage(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynWebPage>().InSingle(id);
    }

    public DynWebPage? FindWebPage(int projectId, string route)
    {
        using var db = _db.Create();
        var r = (route ?? "").Trim();
        if (!r.StartsWith("/")) r = "/" + r;
        return db.Queryable<DynWebPage>()
            .Where(x => x.ProjectId == projectId && x.Route == r && x.IsEnabled)
            .OrderByDescending(x => x.IsHome).First();
    }

    public OpResult SaveWebPage(DynWebPage w)
    {
        if (string.IsNullOrWhiteSpace(w.Name)) return Op(false, "页面名称不能为空");
        if (string.IsNullOrWhiteSpace(w.Route)) return Op(false, "路由不能为空");
        if (w.TemplateId <= 0) return Op(false, "请选择模板");
        using var db = _db.Create();
        w.Route = w.Route.Trim().StartsWith("/") ? w.Route.Trim() : "/" + w.Route.Trim();
        w.UpdatedAt = DateTime.UtcNow;
        if (w.Id == 0)
        {
            if (db.Queryable<DynWebPage>().Any(x => x.ProjectId == w.ProjectId && x.Route == w.Route))
                return Op(false, "同工程已存在该路由");
            w.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(w).ExecuteReturnIdentity();
            return Op(true, "保存成功", new { Id = (int)id });
        }
        db.Updateable(w).ExecuteCommand();
        return Op(true, "保存成功", w);
    }

    public OpResult DeleteWebPage(int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynWebPage>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    public static DynTemplateConfig? ParseTemplateConfig(DynTemplate? t)
    {
        if (t == null || string.IsNullOrWhiteSpace(t.Config)) return null;
        try { return Newtonsoft.Json.JsonConvert.DeserializeObject<DynTemplateConfig>(t.Config); }
        catch { return null; }
    }

    public static DynWebPageConfig? ParseWebPageConfig(DynWebPage? w)
    {
        if (w == null || string.IsNullOrWhiteSpace(w.Config)) return null;
        try { return Newtonsoft.Json.JsonConvert.DeserializeObject<DynWebPageConfig>(w.Config); }
        catch { return null; }
    }

    /// <summary>解析模板参数定义（ParamSchema JSON → 参数项列表）</summary>
    public static List<DynTemplateParam> ParseParamSchema(DynTemplate? t)
    {
        if (t == null || string.IsNullOrWhiteSpace(t.ParamSchema)) return new();
        try { return Newtonsoft.Json.JsonConvert.DeserializeObject<List<DynTemplateParam>>(t.ParamSchema) ?? new(); }
        catch { return new(); }
    }

    /// <summary>解析页面实例参数（Params JSON → 字典）</summary>
    public static Dictionary<string, object?> ParseParams(DynWebPage? w)
    {
        var dict = new Dictionary<string, object?>();
        if (w == null || string.IsNullOrWhiteSpace(w.Params)) return dict;
        try
        {
            var j = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object?>>(w.Params);
            if (j != null) foreach (var kv in j) dict[kv.Key] = kv.Value;
        }
        catch { }
        return dict;
    }

    /// <summary>把参数值转为 int?（兼容 int/long/JValue/字符串）</summary>
    public static int? ToIntParam(object? v)
    {
        if (v == null) return null;
        if (v is int i) return i;
        if (v is long l) return (int)l;
        if (v is Newtonsoft.Json.Linq.JValue jv)
            return int.TryParse(jv.Value?.ToString(), out var r) ? r : null;
        return int.TryParse(v.ToString(), out var n) ? n : null;
    }

    /// <summary>生效的三屏 Id：页面实例 Params > 页面 Config 覆盖 > 模板配置（未指定为 null）</summary>
    public static (int? filter, int? summary, int? detail) EffectivePageIds(DynWebPage? w, DynTemplate? t)
    {
        var ps = ParseParams(w);
        var f = ps.TryGetValue("filterPageId", out var fv) ? ToIntParam(fv) : null;
        var s = ps.TryGetValue("summaryPageId", out var sv) ? ToIntParam(sv) : null;
        var d = ps.TryGetValue("detailPageId", out var dv) ? ToIntParam(dv) : null;
        if (f == null && s == null && d == null)
        {
            // 兼容旧数据：未配置 Params 时回退页面 Config 覆盖 / 模板三字段
            var wc = ParseWebPageConfig(w);
            return (
                wc?.FilterPageId ?? t?.FilterPageId,
                wc?.SummaryPageId ?? t?.SummaryPageId,
                wc?.DetailPageId ?? t?.DetailPageId);
        }
        return (f, s, d);
    }

    // ==================== 外键导航自动检测 ====================

    /// <summary>
    /// 按命名约定自动检测外键导航建议：
    /// ManyToOne：当前表存在形如 {X}Id 的列且 X 是库中表（如 DrawingId → Drawing）
    /// OneToMany：其它表中存在列名为 {当前表}Id 的列（如 Component.DrawingId → Drawing）
    /// </summary>
    public List<DynNavConfig> BuildNavSuggestions(ISqlSugarClient db, string table)
    {
        var navs = new List<DynNavConfig>();
        HashSet<string> tables;
        try { tables = db.DbMaintenance.GetTableInfoList(false).Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase); }
        catch { return navs; }

        var cols = _crud.GetColumns(db, table);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ManyToOne：当前表列 {X}Id → 目标表 X
        foreach (var c in cols)
        {
            var m = Regex.Match(c.DbColumnName, "^(.+?)(?:Id|ID)$");
            if (!m.Success) continue;
            var target = m.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(target) || !tables.Contains(target)) continue;
            if (string.Equals(target, table, StringComparison.OrdinalIgnoreCase)) continue;
            if (seen.Contains("M:" + target)) continue;
            seen.Add("M:" + target);
            navs.Add(new DynNavConfig
            {
                NavKey = target,
                Label = target,
                Relation = NavRelation.ManyToOne,
                TargetTable = target,
                FkColumn = c.DbColumnName
            });
        }

        // OneToMany：其它表存在列 {table}Id 指向当前表
        foreach (var t in tables)
        {
            if (string.Equals(t, table, StringComparison.OrdinalIgnoreCase)) continue;
            List<DbColumnInfo> tcols;
            try { tcols = _crud.GetColumns(db, t); } catch { continue; }
            var fk = tcols.FirstOrDefault(x =>
                string.Equals(x.DbColumnName, table + "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.DbColumnName, table + "ID", StringComparison.OrdinalIgnoreCase));
            if (fk == null) continue;
            if (seen.Contains("O:" + t)) continue;
            seen.Add("O:" + t);
            navs.Add(new DynNavConfig
            {
                NavKey = t,
                Label = t,
                Relation = NavRelation.OneToMany,
                TargetTable = t,
                TargetFkColumn = fk.DbColumnName
            });
        }

        return navs;
    }

    // ==================== SQL 视图生成 ====================

    /// <summary>为该页面生成 CREATE VIEW 脚本（在工程库中执行）</summary>
    public OpResult BuildSqlView(DynProject p, DynPage page, DynPageDefinition def)
    {
        var viewName = "v" + page.Name;
        var cols = new List<string>();
        foreach (var c in def.Columns.Where(x => x.IsGrid || x.IsPrimary))
            cols.Add($"[{c.Name}]");
        if (cols.Count == 0) cols.Add("*");
        var sql = $"IF OBJECT_ID('dbo.{viewName}','V') IS NOT NULL DROP VIEW dbo.{viewName};\n" +
                  $"GO\nCREATE VIEW dbo.{viewName} AS\nSELECT {string.Join(",\n       ", cols)}\nFROM dbo.[{page.TableName}];\nGO";
        return Op(true, "ok", sql);
    }

    private static OpResult Op(bool ok, string msg, object? data = null) => new() { Success = ok, Message = msg, Data = data };
}
